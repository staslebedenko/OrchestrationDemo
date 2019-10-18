using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace OrchestrationDemo
{
    public static class Loader
    {
        [FunctionName("Loader")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "loaderio-1a9e53e1b54a2cd126eb9ba81c5601ba")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Loader.io validation triggered.");
            return (ActionResult)new OkObjectResult($"loaderio-1a9e53e1b54a2cd126eb9ba81c5601ba");
        }
    }
}
