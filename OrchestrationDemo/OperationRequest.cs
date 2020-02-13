using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace OrchestrationDemo
{
    public static class OperationRequest
    {
        [FunctionName("Zoltar")]
        public static async Task<IActionResult> Zoltar(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "askZoltar/{name}")] HttpRequest req,
            string name,
            ILogger log,
            [Queue("zoltar-requests", Connection = "StorageConnectionString")] IAsyncCollector<string> messages)
        {
            await messages.AddAsync(name);

            return name != null
                ? (ActionResult)new OkObjectResult($"Dear {name}, you request to Zoltar is accepted")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }
    }
}
