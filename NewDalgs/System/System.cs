using NewDalgs.Core;
using NewDalgs.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NewDalgs.System
{
    class System
    {
        private readonly Communication.ProcessId _processId;
        private readonly string _hubHost;
        private readonly int _hubPort;

        private NetworkHandler _networkHandler;

        public System(Communication.ProcessId processId, string hubHost, int hubPort)
        {
            _processId = processId;
            _hubHost = hubHost;
            _hubPort = hubPort;
            _networkHandler = new NetworkHandler(processId.Host, processId.Port);
        }

        public void Start()
        {
            var listener = Task.Run(() =>
                {
                    try
                    {
                        _networkHandler.ListenForConnections();
                    }
                    catch (NetworkException ex)
                    {
                        // TODO notify stop of the system
                    }
                });
            
            RegisterToHub();

            //Thread.Sleep(10000);
            //Stop();

            listener.Wait();
        }

        public void Stop()
        {
            _networkHandler.StopListener();
            // TODO would be nice to notify dalgs that process is unregistered
            // TODO move listener.Wait here
        }

        private void RegisterToHub()
        {
            var procRegistration = new Communication.ProcRegistration
            {
                Owner = _processId.Owner,
                Index = _processId.Index
            };

            var wrapperMsg = new Communication.Message
            {
                Type = Communication.Message.Types.Type.ProcRegistration,
                ProcRegistration = procRegistration,
                //SystemId = "sys-1",     // TODO should be added?!
                ToAbstractionId = "app",
                MessageUuid = Guid.NewGuid().ToString()
            };

            _networkHandler.SendMessage(wrapperMsg, _hubHost, _hubPort);
        }
    }
}
