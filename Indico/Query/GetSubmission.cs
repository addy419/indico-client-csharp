﻿using GraphQL.Common.Request;
using GraphQL.Common.Response;
using Indico.Entity;
using Indico.Exception;
using Indico.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace Indico.Query
{
    public class GetSubmission : Query<Submission>
    {
        IndicoClient _client;
        public int Id { get; set; }

        public GetSubmission(IndicoClient client) => this._client = client;

        /// <summary>
        /// Queries the server and returns Submission
        /// </summary>
        /// <returns>Submission</returns>
        async public Task<Submission> Exec()
        {
            string query = @"
                    query GetSubmission($submissionId: Int!){
                        submission(id: $submissionId){
                            id
                            datasetId
                            workflowId
                            status
                            inputFile
                            inputFilename
                            resultFile
                            retrieved
                        }
                    }
                ";
            GraphQLRequest request = new GraphQLRequest()
            {
                Query = query,
                OperationName = "GetSubmission",
                Variables = new
                {
                    submissionId = this.Id
                }
            };

            GraphQLResponse response = await this._client.GraphQLHttpClient.SendQueryAsync(request);
            if (response.Errors != null)
            {
                throw new GraphQLException(response.Errors);
            }

            JObject submission = response.Data.submission;
            return new Submission()
            {
                Id = submission.Value<int>("id"),
                DatasetId = submission.Value<int>("datasetId"),
                WorkflowId = submission.Value<int>("workflowId"),
                Status = (SubmissionStatus)Enum.Parse(typeof(SubmissionStatus), submission.Value<string>("status")),
                InputFile = submission.Value<string>("inputFile"),
                InputFilename = submission.Value<string>("inputFilename"),
                ResultFile = submission.Value<string>("resultFile"),
                Retrieved = submission.Value<bool>("retrieved"),
                Errors = submission.Value<string>("errors")
            };
        }

        async public Task<Submission> Refresh(Submission obj)
        {
            //TODO:
            throw new RuntimeException("Method Not Implemented");
        }
    }
}
