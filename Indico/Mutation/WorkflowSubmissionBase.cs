﻿using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GraphQL.Common.Request;
using GraphQL.Common.Response;
using Indico.Exception;
using Indico.Storage;
using Newtonsoft.Json.Linq;

namespace Indico.Mutation
{
    public class WorkflowSubmissionBase : Mutation<JObject>
    {
        IndicoClient _client;
        /// <summary>
        /// Workflow Id
        /// </summary>
        /// <value>Workflow Id</value>
        public virtual int WorkflowId { get; set; }
        /// <summary>
        /// Files to submit
        /// </summary>
        /// <value>Files</value>
        public virtual List<string> Files { get; set; }
        public virtual List<Stream> Streams { get; set; }
        public virtual List<string> Urls { get; set; }
        protected virtual bool Detailed { get; set; }

        protected WorkflowSubmissionBase(IndicoClient client) => this._client = client;

        /// <summary>
        /// Executes request and returns Job
        /// </summary>
        /// <returns>Job</returns>
        async public Task<JObject> Exec()
        {
            if (this.Files == null && this.Streams == null && this.Urls == null)
            {
                throw new InputException("One of 'Files', 'Streams' or 'Urls' must be specified");
            }
            else if (this.Files != null && this.Streams != null && this.Urls != null)
            {
                throw new InputException("Only one of 'Files', 'Streams' or 'Urls' must be specified");
            }

            List<object> files = new List<object>();
            string arg, type, mutationName;

            if(this.Files != null || this.Streams != null)
            {
                arg = "files";
                type = "[FileInput]!";
                mutationName = "workflowSubmission";

                JArray fileMetadata;

                if (this.Files != null)
                {
                    UploadFile uploadRequest = new UploadFile(this._client)
                    {
                        Files = this.Files
                    };
                    fileMetadata = await uploadRequest.Call();
                }
                else
                {
                    UploadStream uploadRequest = new UploadStream(this._client)
                    {
                        Streams = this.Streams
                    };
                    fileMetadata = await uploadRequest.Call();
                }

                foreach (JObject uploadMeta in fileMetadata)
                {
                    JObject meta = new JObject
                    {
                        { "name", uploadMeta.Value<string>("name") },
                        { "path", uploadMeta.Value<string>("path") },
                        { "upload_type", uploadMeta.Value<string>("upload_type") }
                    };

                    var file = new
                    {
                        filename = uploadMeta.Value<string>("name"),
                        filemeta = meta.ToString()
                    };

                    files.Add(file);
                }
            }
            else
            {
                arg = "urls";
                type = "[String]!";
                mutationName = "workflowUrlSubmission";
            }

            string query = $@"
                    mutation WorkflowSubmission($workflowId: Int!, ${arg}: {type}, $recordSubmission: Boolean) {{
                        {mutationName}(workflowId: $workflowId, {arg}: ${arg}, recordSubmission: $recordSubmission) {{
                            jobIds
                            submissionIds
                        }}
                    }}
                ";

            string queryDetailed = $@"
                    mutation workflowSubmissionMutation($workflowId: Int!, ${arg}: {type}, $recordSubmission: Boolean) {{
                        {mutationName}(workflowId: $workflowId, {arg}: ${arg}, recordSubmission: $recordSubmission) {{
                            submissionIds
                            submissions {{
                                id
                                datasetId
                                workflowId
                                status
                                inputFile
                                inputFilename
                                resultFile
                                retrieved
                                errors
                            }}
                        }}
                    }}
                ";

            GraphQLRequest request = new GraphQLRequest()
            {
                Query = this.Detailed ? queryDetailed : query,
                OperationName = "WorkflowSubmission",
                Variables = new
                {
                    workflowId = this.WorkflowId,
                    files = files,
                    urls = this.Urls
                }
            };

            GraphQLResponse response = await this._client.GraphQLHttpClient.SendMutationAsync(request);
            if (response.Errors != null)
            {
                throw new GraphQLException(response.Errors);
            }

            JObject data = response.Data;
            return (JObject) data.GetValue(mutationName);
        }
    }
}