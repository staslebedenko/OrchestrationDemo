using Microsoft.Extensions.Logging;
using Polly;

namespace OrchestrationDemo
{
    public static class PollyContextExtension
    {
        private static readonly string loggerKey = "LoggerKey";

        public static Context WithLogger(this Context context, ILogger logger)
        {
            context[loggerKey] = logger;
            return context;
        }

        public static ILogger TryGetLogger(this Context context)
        {

            if (context.TryGetValue(loggerKey, out object logger))
            {
                return logger as ILogger;
            }

            return null;
        }
    }
}