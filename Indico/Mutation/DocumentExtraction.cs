﻿using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL.Common.Request;
using GraphQL.Common.Response;
using Indico.Exception;
using Indico.Jobs;
using Indico.Storage;
using Newtonsoft.Json.Linq;

namespace Indico.Mutation
{
    /// <summary>
    /// OCR PDF, TIF, JPG and PNG files
    /// </summary>
    public class DocumentExtraction : Mutation<List<Job>>
    {
        IndicoClient _client;

        /// <summary>
        /// List of files to process
        /// </summary>
        public List<string> Files { get; set; }

        /// <summary>
        /// Get/Set the JSON Configuration for DocumentExtraction
        /// </summary>
        public JObject JsonConfig { get; set; }

        /// <summary>
        /// DocumentExtraction constructor
        /// <param name="client">IndicoClient client</param>
        /// </summary>
        public DocumentExtraction(IndicoClient client)
        {
            this._client = client;
        }

        async private Task<JArray> Upload(List<string> filePaths)
        {
            UploadFile uploadRequest = new UploadFile(this._client) { Files = filePaths };
            var arr = await uploadRequest.Call();
            return arr;
        }

        async private Task<GraphQLResponse> ExecRequest()
        {
            JArray fileMetadata;
            List<object> files = new List<object>();
            fileMetadata = await this.Upload(this.Files);
            foreach (JObject uploadMeta in fileMetadata)
            {
                JObject meta = new JObject
                {
                    { "name", (string)uploadMeta.GetValue("name") },
                    { "path", (string)uploadMeta.GetValue("path") },
                    { "upload_type", (string)uploadMeta.GetValue("upload_type") }
                };

                var file = new
                {
                    filename = (string)uploadMeta.GetValue("name"),
                    filemeta = meta.ToString()
                };

                files.Add(file);
            }

            string query = @"
                    mutation DocumentExtraction($files: [FileInput]!, $jsonConfig: JSONString) {
                        documentExtraction(files: $files, jsonConfig: $jsonConfig) {
                            jobIds
                        }
                    }
                ";


            GraphQLRequest request = new GraphQLRequest()
            {
                Query = query,
                OperationName = "DocumentExtraction",
                Variables = new
                {
                    files,
                    JsonConfig = this.JsonConfig.ToString()
                }
            };

            var response = await this._client.GraphQLHttpClient.SendMutationAsync(request);
            return response;
        }

        /// <summary>
        /// Executes OCR and returns Jobs
        /// <returns>List of Jobs</returns>
        /// </summary>
        async public Task<List<Job>> Exec()
        {
            GraphQLResponse response = await this.ExecRequest();
            if (response.Errors != null)
            {
                throw new GraphQLException(response.Errors);
            }

            JArray jobIds = (JArray)response.Data.documentExtraction.jobIds;
            List<Job> jobs = new List<Job>();
            foreach (string id in jobIds)
            {
                Job job = new Job(this._client.GraphQLHttpClient, id);
                jobs.Add(job);
            }

            return jobs;
        }


        /// <summary>
        /// Executes a single OCR request
        /// <param name="path">pathname of the file to OCR</param>
        /// <returns>Job</returns>
        /// </summary>
        async public Task<Job> Exec(string path)
        {
            this.Files = new List<string>() { path };

            GraphQLResponse response = await this.ExecRequest();
            if (response.Errors != null)
            {
                throw new GraphQLException(response.Errors);
            }

            JArray jobIds = (JArray)response.Data.documentExtraction.jobIds;

            return new Job(this._client.GraphQLHttpClient, (string)jobIds[0]);
        }
    }
}