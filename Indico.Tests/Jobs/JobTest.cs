﻿using GraphQL.Client.Http;
using Indico.Jobs;
using Indico.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Indico.Tests.Mutation
{
    [TestClass]
    public class JobTest
    {
        GraphQLHttpClient _client;

        [TestInitialize]
        public void Initialize()
        {
            GraphQLHttpClientOptions options = new GraphQLHttpClientOptions()
            {
                EndPoint = new System.Uri("http://www.example.com/graph/api/graphql"),
                HttpMessageHandler = new MockHttpHandler()
            };
            _client = new GraphQLHttpClient(options);
        }

        [TestMethod]
        public void Test()
        {
            JobQuery jobQuery = new JobQuery(_client);
            Job job = jobQuery.Id("jobId_test").Query();
            Assert.AreEqual(JobStatus.SUCCESS, job.Status());
            JObject json = (JObject)job.Results().Result[0];
            Assert.AreEqual("testValue", json.GetValue("testKey"));
        }
    }
}
