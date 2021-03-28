using System.Collections.Generic;
using System.Threading.Tasks;

namespace NewDalgs.Core
{
    class Core
    {
        private List<Task> _processes = new List<Task>();
        private List<System.System> _systems = new List<System.System>();

        public Core()
        {
        }

        public void Run(CoreParams coreParams)
        {
            int processIndex = 1;
            foreach (var port in coreParams.ProcessesPorts)
            {
                var processsId = new ProtoComm.ProcessId
                {
                    Host = coreParams.ProcessesHost,
                    Port = port,
                    Owner = coreParams.Owner,
                    Index = processIndex
                };
                processIndex++;

                var system = new System.System(processsId, coreParams.HubHost, coreParams.HubPort);
                var process = Task.Run(() =>
                {
                    system.Start();
                });

                _systems.Add(system);
                _processes.Add(process);
            }
        }

        public void Stop()
        {
            foreach (var system in _systems)
            {
                system.Stop();
            }

            foreach (var process in _processes)
            {
                process.Wait();
            }
        }
    }
}
