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

        private BlockingCollection<ReceivedMessage> _messageQueue;

        private bool _wasStopped = false;

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
            _messageListener = new Task(() =>
                {
                    _networkHandler.ListenForConnections();
                });
            var messageListenerFallbackTask = _messageListener.ContinueWith(MessageListenerFallback);       // Continuation Task should handle exceptions thrown in messageListener Task
            _messageListener.Start();

            try
            {
                RegisterToHub();
                Logger.Info($"[{_processId.Port}]: Process registered - [{_processId.Owner}-{_processId.Index}]");
            }
            catch (NetworkException ex)
            {
                Logger.Fatal($"[{_processId.Port}]: {ex.Message}");
                Stop();     // TODO throw StopProgramException to stop whole program?
            }

            try
            {
                EventLoop();
            }
            catch (Exception ex)
            {
                Logger.Fatal($"[{_processId.Port}]: {ex.Message}");
                Stop();     // TODO throw StopProgramException to stop whole program?
            }

            messageListenerFallbackTask.Wait();
        }

        public void Stop()
        {
            if (_wasStopped)
                return;

            UnsubscribeFromMessageListener();
            _networkHandler.StopListener();     // TODO would be nice to notify dalgs that process is unregistered

            try
            {
                _messageListener.Wait();
            }
            catch (AggregateException) { }      // Exception ignored becaused it is handled by MessageListenerFallback

            _wasStopped = true;
        }

        private void MessageListenerFallback(Task antecedent)
        {
            if (antecedent.Status == TaskStatus.RanToCompletion)
            {
                Logger.Debug($"[{_processId.Port}]: MessageListener stopped");
                return;
            } 
            else if (antecedent.Status == TaskStatus.Faulted)
            { 
                var ex = antecedent.Exception?.GetBaseException();
                Logger.Fatal($"[{_processId.Port}]: {ex.Message}");
                Stop();     // TODO throw StopProgramException to stop whole program?
            }
        }

        private void RegisterToHub()
        {
            if (_wasStopped)
                return;

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
            if (_wasStopped)
                return;
            
            foreach (var msg in _messageQueue.GetConsumingEnumerable())
            {
                Logger.Warn($"[{_processId.Port}]: {msg.Message.Type}");

                try
                {
                    ProcessReceivedMessage(msg);
                }
                catch (Exception ex)        // TODO replace with more specific exception
                {
                    Logger.Error($"[{_processId.Port}]: {ex.Message}");
                    continue;
                }
            }

            Logger.Debug($"[{_processId.Port}]: EventLoop stopped");
        }

        // TODO message handling should be done in separate class - maybe use dict to map type of message to corresponding alg
        private void ProcessReceivedMessage(ReceivedMessage msg)
        {
            if (msg.Message.Type == ProtoComm.Message.Types.Type.ProcInitializeSystem)
            {
                var procInitSysMsg = msg.Message.ProcInitializeSystem;
                foreach (var proc in procInitSysMsg.Processes)
                {
                    if (!_processes.TryAdd(proc.Port, proc))
                    {
                        Logger.Error($"[{_processId.Port}]: Could not add a process from ProcInitializeSystem");
                        // TODO should throw exception?
                    }
                }
            }
            else if (msg.Message.Type == ProtoComm.Message.Types.Type.ProcDestroySystem)
            {
                _processes.Clear();
            }
            else
            {
                var wrappedMsg = WrapIntoPLDeliver(msg);
                Logger.Debug($"[{_processId.Port}]: {wrappedMsg.PlDeliver.Sender}");
            }
        }

        /// <summary>
        /// Wrapping received message into Message(PlDeliver)
        /// </summary>
        private ProtoComm.Message WrapIntoPLDeliver(ReceivedMessage msg)
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

            return outMsg;
        }
    }
}
