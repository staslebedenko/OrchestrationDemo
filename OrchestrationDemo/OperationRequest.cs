using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace OrchestrationDemo
{
    public class OperationRequest
    {
        private CloudQueue queue;

        public OperationRequest()
        {
            var storageString = Environment.GetEnvironmentVariable("StorageConnectionString");
            var account = CloudStorageAccount.Parse(storageString);
            var queueClient = account.CreateCloudQueueClient();
            this.queue = queueClient.GetQueueReference("zoltar-results");
        }

        [FunctionName("Zoltar")]
        public async Task<IActionResult> Zoltar(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "askZoltar/{name}")] HttpRequest req,
            string name,
            ILogger log,
            [Queue("zoltar-requests", Connection = "StorageConnectionString")] IAsyncCollector<string> messages)
        {
            await messages.AddAsync(name);

            IEnumerable<CloudQueueMessage> results = await this.queue.GetMessagesAsync(32);

            var predictions = string.Join(",", results.Select(x => x.AsString));

            return name != null
                ? (ActionResult)new OkObjectResult($"Dear {name}, you request to Zoltar is accepted. {Environment.NewLine} Previous predictions: {predictions}")
                : new BadRequestObjectResult($"Please pass a name on the query string or in the request body. {Environment.NewLine} Previous predictions: {predictions}");
        }
    }
}
