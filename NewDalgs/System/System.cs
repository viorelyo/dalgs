using NewDalgs.Networking;
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

        private ConcurrentDictionary<int, ProtoComm.ProcessId> _processes;

        private Task _messageListener;
        private NetworkHandler _networkHandler;

        //private Task _eventLoopTask;
        private BlockingCollection<ReceivedMessage> _messageQueue;

        public System(ProtoComm.ProcessId processId, string hubHost, int hubPort)
        {
            _processId = processId;
            _hubHost = hubHost;
            _hubPort = hubPort;

            _processes = new ConcurrentDictionary<int, ProtoComm.ProcessId>();

            _networkHandler = new NetworkHandler(processId.Host, processId.Port);
            _messageQueue = new BlockingCollection<ReceivedMessage>(new ConcurrentQueue<ReceivedMessage>());
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

        protected virtual void OnMessageReceived(NetworkHandler p, ReceivedMessage e)
        {
            _messageQueue.Add(e);
            Logger.Info($"[{_processId.Port}]: Message added - [{e.Message.Type}]");
        }

        private void EventLoop()
        {
            // TODO maybe move try/catch into foreach -> on exception ignore message and continue with next msg
            try
            {
                foreach (var msg in _messageQueue.GetConsumingEnumerable())
                {
                    Logger.Warn($"[{_processId.Port}]: {msg.Message.Type}");

                    ProcessReceivedMessage(msg);
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex);
                // TODO notify stop of the system && remove process from list!
            }
        }


        /// <summary>
        /// Wrapping received message into Message(PlDeliver)
        /// </summary>
        private void ProcessReceivedMessage(ReceivedMessage msg)
        {
            var plDeliverMsg = new ProtoComm.PlDeliver
            {
                Message = msg.Message
            };

            ProtoComm.ProcessId foundProcessId;
            if (_processes.TryGetValue(msg.SenderListeningPort, out foundProcessId))
            {
                plDeliverMsg.Sender = foundProcessId;
            }

            var outMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.PlDeliver,
                PlDeliver = plDeliverMsg,
                SystemId = msg.ReceivedSystemId,
                ToAbstractionId = msg.ReceivedToAbstractionId
            };


        }
    }
}
