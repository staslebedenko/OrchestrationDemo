using System;

namespace OrchestrationDemo
{
    public static class EnvironmentHelper
    {
        public static bool IsProduction => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";

        public static bool IsStaging => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Staging";
    }
}