using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ServerlessWorkshop
{
    public static class Orchestrator
    {
        [FunctionName("Orchestrator")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            outputs.Add(await context.CallActivityAsync<string>("FortuneTeller", "name"));
            outputs.Add(await context.CallActivityAsync<string>("FortuneTeller", "name"));
            outputs.Add(await context.CallActivityAsync<string>("FortuneTeller", "name"));

            return outputs;
        }

        [FunctionName("Orchestrator_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            [Blob("unique-permission-reports", Connection = "BlobStorageConnStr")] CloudBlobContainer blobContainer,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("Orchestrator", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
            var timeout = TimeSpan.FromSeconds(20);
            var retryInterval = TimeSpan.FromSeconds(1); // How often to check the orchestration instance for completion
            var result = await starter.WaitForCompletionOrCreateCheckStatusResponseAsync(req, instanceId, timeout, retryInterval);
            return result;
            // return starter.CreateCheckStatusResponse(req, instanceId).;
        }

        [FunctionName("FortuneTeller")]
        public static string FortuneTeller([ActivityTrigger] string name, ILogger log)
        {
            var random = new Random();
            var rate = random.Next(25, 100);
            var prediction = $"Next year rate for {name} is {rate}";
            log.LogInformation(prediction);
            return prediction;
        }
    }
}