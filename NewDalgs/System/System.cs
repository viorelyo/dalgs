using NewDalgs.Utils;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace NewDalgs.System
{
    class System
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly ProtoComm.ProcessId _processId;
        private readonly string _hubHost;
        private readonly int _hubPort;

        private Task _messageListener;
        private NetworkHandler _networkHandler;

        private Task _eventLoopTask;
        private BlockingCollection<ProtoComm.Message> _messageQueue;

        public System(ProtoComm.ProcessId processId, string hubHost, int hubPort)
        {
            _processId = processId;
            _hubHost = hubHost;
            _hubPort = hubPort;
            _networkHandler = new NetworkHandler(processId.Host, processId.Port);
            _messageQueue = new BlockingCollection<ProtoComm.Message>(new ConcurrentQueue<ProtoComm.Message>());
        }

        public void Start()
        {
            _messageListener = Task.Run(() =>
                {
                    try
                    {
                        _networkHandler.ListenForConnections();
                    }
                    catch (NetworkException ex)
                    {
                        Logger.Fatal(ex);
                        // TODO notify stop of the system && remove process from list!
                    }
                });

            try
            {
                RegisterToHub();
            }
            catch (NetworkException ex)
            {
                Logger.Fatal(ex);
                // TODO notify stop of the system && remove process from list!
            }

            Logger.Info($"[{_processId.Port}]: Process registered - [{_processId.Owner}-{_processId.Index}]");

            _eventLoopTask = Task.Run(this.EventLoop);      // TODO decide if separate Task is needed
        }

        public void Stop()
        {
            _networkHandler.StopListener();     // TODO would be nice to notify dalgs that process is unregistered
            _messageQueue.CompleteAdding();

            _messageListener.Wait();
            _eventLoopTask.Wait();
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

        private void EventLoop()
        {
            // TODO try/catch excpetions
            foreach (var msg in _messageQueue.GetConsumingEnumerable())
            {
                Logger.Info($"{msg.Type}");
            }
        }
    }
}
