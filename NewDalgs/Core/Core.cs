using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using NewDalgs.System;

namespace NewDalgs.Core
{
    class Core
    {
        private readonly ILogger logger;

        public Core(ILogger<Core> logger_)
        {
            logger = logger_;
        }

        public void Run(CoreParams coreParams)
        {
            logger.LogTrace("Starting processes");

            // TODO create threads for each process
            var system = new System.System();
            system.Start();
        }
    }
}
