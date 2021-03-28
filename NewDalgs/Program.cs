using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NewDalgs.Core;
using System;
using System.Collections.Generic;

namespace NewDalgs
{
    class Program
    {
        // TODO handle CTRL-C

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
            if (coreParams == null)
                Environment.Exit(1);

            //var core = serviceProvider.GetService<Core.Core>();
            var core = new Core.Core();
            core.Run(coreParams);
        }

        static CoreParams ValidateInput(string[] args)
        {
            if (args.Length < 7)
            {
                
                return null;
            }

            var coreParams = new CoreParams();
            // TODO validate input
            coreParams.HubHost = args[0];

            int parsedInt;
            if (!Int32.TryParse(args[1], out parsedInt))
            {

                return null;
            }

            coreParams.HubPort = parsedInt;
            coreParams.ProcessesHost = args[2];
            coreParams.ProcessesPorts = new List<int>();

            if (!Int32.TryParse(args[3], out parsedInt))
            {

                return null;
            }
            coreParams.ProcessesPorts.Add(parsedInt);

            if (!Int32.TryParse(args[4], out parsedInt))
            {

                return null;
            }
            coreParams.ProcessesPorts.Add(parsedInt);

            if (!Int32.TryParse(args[5], out parsedInt))
            {

                return null;
            }
            coreParams.ProcessesPorts.Add(parsedInt);

            coreParams.Owner = args[6];

            return coreParams;
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
