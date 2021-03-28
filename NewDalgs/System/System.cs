using NewDalgs.Utils;
using System;
using System.Threading.Tasks;

namespace NewDalgs.System
{
    class System
    {
        private readonly ProtoComm.ProcessId _processId;
        private readonly string _hubHost;
        private readonly int _hubPort;

        private NetworkHandler _networkHandler;

        public System(ProtoComm.ProcessId processId, string hubHost, int hubPort)
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
            var procRegistration = new ProtoComm.ProcRegistration
            {
                Owner = _processId.Owner,
                Index = _processId.Index
            };

            var wrapperMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.ProcRegistration,
                ProcRegistration = procRegistration,
                //SystemId = "sys-1",     // TODO should be added?!
                ToAbstractionId = "app",
                MessageUuid = Guid.NewGuid().ToString()
            };

            _networkHandler.SendMessage(wrapperMsg, _hubHost, _hubPort);
        }
    }
}
