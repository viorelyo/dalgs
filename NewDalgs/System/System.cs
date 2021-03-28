using NewDalgs.Utils;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NewDalgs.System
{
    class System
    {
        private NetworkHandler _networkHandler;

        public System()
        {
            _networkHandler = new NetworkHandler("127.0.0.1", 5004);
        }

        public void Start()
        {
            var listener = Task.Run(() =>
                {
                    try
                    {
                        _networkHandler.ListenForConnections();
                    }
                    catch (SocketException ex)
                    {
                        _networkHandler.StopListener();
                        // TODO notify stop of the system
                    }
                });
            
            RegisterToHub();

            Thread.Sleep(3000);
            _networkHandler.StopListener();

            listener.Wait();
        }

        private void RegisterToHub()
        {
            var procRegistration = new Communication.ProcRegistration
            {
                Owner = "gvsd",     // TODO extract from coreParams
                Index = 1
            };

            var wrapperMsg = new Communication.Message
            {
                Type = Communication.Message.Types.Type.ProcRegistration,
                ProcRegistration = procRegistration,
                SystemId = "sys-1",     // TODO extract from coreParams
                ToAbstractionId = "app",
                MessageUuid = Guid.NewGuid().ToString()
            };

            _networkHandler.SendMessage(wrapperMsg, "127.0.0.1", 5000);     // TODO exctract from coreParams
        }
    }
}
