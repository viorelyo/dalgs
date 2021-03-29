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
            SubscribeToMessageListener();
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

            //_eventLoopTask = Task.Run(this.EventLoop);      // TODO decide if separate Task is needed
            this.EventLoop();
        }

        public void Stop()
        {
            UnsubscribeFromMessageListener();
            _networkHandler.StopListener();     // TODO would be nice to notify dalgs that process is unregistered

            _messageListener.Wait();
            //_eventLoopTask.Wait();
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

        private void SubscribeToMessageListener()
        {
            _networkHandler.OnPublish += OnMessageReceived;
        }

        private void UnsubscribeFromMessageListener()
        {
            _networkHandler.OnPublish -= OnMessageReceived;
            _messageQueue.CompleteAdding();
        }

        protected virtual void OnMessageReceived(NetworkHandler p, ProtoComm.Message e)
        {
            _messageQueue.Add(e);
            Logger.Info($"[{_processId.Port}]: Message added - [{e.Type}]");
        }

        private void EventLoop()
        {
            try
            {
                foreach (var msg in _messageQueue.GetConsumingEnumerable())
                {
                    Logger.Warn($"[{_processId.Port}]: {msg.Type}");
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex);
                // TODO notify stop of the system && remove process from list!
            }
        }
    }
}
