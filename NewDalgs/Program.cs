using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NewDalgs.Core;
using System;

namespace NewDalgs
{
    class Program
    {
        static void Main(string[] args)
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();

            //var logger = serviceProvider.GetService<ILogger<Program>>();
            //logger.LogInformation("Hello world!");
            //logger.LogWarning("warn");
            //logger.LogError("err");
            //logger.LogDebug("debug");
            //logger.LogTrace("trace");
            //logger.LogCritical("critical");

            var coreParams = ValidateInput(args);

            var core = serviceProvider.GetService<Core.Core>();
            core.Run(coreParams);
        }

        static CoreParams ValidateInput(string[] args)
        {
            // TODO validate input
            //if (args.Length < 5)
            return new CoreParams();
        }

        static void ConfigureServices(ServiceCollection services)
        {
            services.AddLogging(loggerBuilder =>
            {
                loggerBuilder.ClearProviders();
                loggerBuilder.AddConsole();
            });
            
            services.AddTransient<Core.Core>();
        }
    }
}
