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
    public class ZoltarPrediction
    {
        private CloudQueue queue;

        public ZoltarPrediction()
        {
            var storageString = Environment.GetEnvironmentVariable("StorageConnectionString");
            var account = CloudStorageAccount.Parse(storageString);
            var queueClient = account.CreateCloudQueueClient();
            this.queue = queueClient.GetQueueReference("zoltar-results");
        }

        [FunctionName("ZoltarSpeaks")]
        public async Task<IActionResult> ZoltarSpeaks(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ZoltarSpeaks")] HttpRequest req,
            ILogger log)
            {
                IEnumerable<CloudQueueMessage> messages = await this.queue.GetMessagesAsync(32);

                return (ActionResult) new OkObjectResult(messages.Select(x=>x.AsString));
        } 
        
        [FunctionName("PostMessage")]
        public async Task<IActionResult> PostMessage(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "PostMessage")] HttpRequest req,
            ILogger log)
            {
                var newMessage = new CloudQueueMessage("test message");
                await this.queue.AddMessageAsync(newMessage, TimeSpan.FromSeconds(-1), null, null, null);

                return (ActionResult) new OkObjectResult("Success.");
        }
    }
}
