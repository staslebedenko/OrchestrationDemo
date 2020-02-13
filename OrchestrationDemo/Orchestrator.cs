using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace OrchestrationDemo
{
    public static class Orchestrator
    {
        //public Orchestrator(ILogger<FortuneTellerController> log,
        //    AsyncRetryPolicy retryPolicy)
        //{
        //    this.log = log;
        //    this.retryPolicy = retryPolicy;
        //}

        [FunctionName("Orchestrator")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var name = context.GetInput<Person>();

            var outputs = new List<string>();

            outputs.Add(await context.CallActivityAsync<string>("FortuneTeller", name));
            outputs.Add(await context.CallActivityAsync<string>("FortuneTeller", name));
            outputs.Add(await context.CallActivityAsync<string>("FortuneTeller", name));
            context.SetCustomStatus("Completed");

            return outputs;
        }
       
        [FunctionName("Orchestrator_Start")]
        public static async void StartOrchestrator(
            [QueueTrigger("incoming-requests", Connection = "StorageConnectionString")] string name,
            [DurableClient] IDurableOrchestrationClient starter,
            [Queue("zoltar-results", Connection = "StorageConnectionString")] IAsyncCollector<string> messages,
            ILogger log)
        {

            //var context = new Context().WithLogger(this.log);
            //var prediction = string.Empty;

            //await this.retryPolicy.ExecuteAsync(async ctx => { prediction = this.ZoltarSpeaks(name); }, context);


            var person = new Person { Name = name };

            var instanceId = await starter.StartNewAsync("Orchestrator", person);

            var status = await starter.GetStatusAsync(instanceId);

            while (status.CustomStatus.ToString() != "Completed")
            {
                await Task.Delay(200);
                status = await starter.GetStatusAsync(instanceId);
            }

            var prediction = $"Zoltar speaks! {name}, your rate will be on of those '{status.Output}'.";

            await messages.AddAsync(prediction);

            log.LogInformation(prediction);
        }

        [FunctionName("FortuneTeller")]
        public static int FortuneTeller([ActivityTrigger] string name, ILogger log)
        {
            var random = new Random();
            return random.Next(25, 75);
        }
    }
}