using NewDalgs.Core;
using System;
using System.Collections.Generic;

namespace NewDalgs
{
    class Program
    {
        // TODO handle CTRL-C

        // maybe reconfigure logger on the fly?!
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
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
    }
}
