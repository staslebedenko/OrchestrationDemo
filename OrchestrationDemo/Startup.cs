using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrchestrationDemo;
using Polly;

[assembly: FunctionsStartup(typeof(Startup))]

namespace OrchestrationDemo
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddLogging(options =>
            {
                options.AddFilter("Functions", LogLevel.Information);
            });

            var sqlString = Environment.GetEnvironmentVariable("SqlConnectionString");
            var password = Environment.GetEnvironmentVariable("SqlConnectionPassword");
            //var connectionString = SqlConnectionBuilder.GetConnectionString(sqlString, password);

            //builder.Services.AddDbContextPool<FunctionDbContext>(
            //builder.Services.AddDbContext<FunctionDbContext>(
            //   options =>
            //       {
            //           if (!string.IsNullOrEmpty(connectionString))
            //           {
            //               options.UseSqlServer(connectionString, providerOptions => providerOptions.EnableRetryOnFailure());
            //           }
            //       });

            var signingKey = string.Empty;

            builder.Services.AddSingleton(sp => {
                return Policy
                    .Handle<Exception>()
                    .WaitAndRetryAsync(2,
                        retryAttempt => TimeSpan.FromSeconds(8),
                        onRetry: (ex, retryCount, context) => {

                            var log = context.TryGetLogger();
                            log?.LogInformation($"Polly retry {retryCount} for exception {ex}");
                        });
            });
        }
    }
}
