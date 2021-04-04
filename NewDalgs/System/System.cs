using Google.Protobuf;
using NewDalgs.Abstractions;
using NewDalgs.Networking;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NewDalgs.System
{
    class System
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public ProtoComm.ProcessId ProcessId { get; private set; }
        public ProtoComm.ProcessId HubProcessId { get; private set; }

        public HashSet<ProtoComm.ProcessId> Processes { get; private set; }

        private ConcurrentDictionary<string, Abstraction> _abstractions;

        private Task _messageListener;
        private NetworkHandler _networkHandler;

        private BlockingCollection<ProtoComm.Message> _messageQueue;

        private bool _wasStopped = false;

        public System(ProtoComm.ProcessId processId, ProtoComm.ProcessId hubProcesId)
        {
            ProcessId = processId;
            HubProcessId = hubProcesId;

            Processes = new HashSet<ProtoComm.ProcessId>();

            _abstractions = new ConcurrentDictionary<string, Abstraction>();

            _networkHandler = new NetworkHandler(processId.Host, processId.Port);
            _messageQueue = new BlockingCollection<ProtoComm.Message>(new ConcurrentQueue<ProtoComm.Message>());
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
                Logger.Info($"[{ProcessId.Port}]: Process registered - [{ProcessId.Owner}-{ProcessId.Index}]");
            }
            catch (NetworkException ex)
            {
                Logger.Fatal($"[{ProcessId.Port}]: {ex.Message}");
                Stop();     // TODO throw StopProgramException to stop whole program?
            }

            RegisterAbstraction(new Application(Application.Name, this));

            try
            {
                EventLoop();
            }
            catch (Exception ex)
            {
                Logger.Fatal($"[{ProcessId.Port}]: {ex.Message}");
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

        public ProtoComm.ProcessId FindProcessByHostAndPort(string host, int port)
        {
            foreach (var proc in Processes)
            {
                if ((proc.Host == host) && (proc.Port == port))
                    return proc;
            }

            return null;
        }

        public void RegisterAbstraction(Abstraction abstraction)
        {
            _abstractions.TryAdd(abstraction.GetId(), abstraction);
        }

        public void AddToMessageQueue(ProtoComm.Message e)
        {
            _messageQueue.Add(e);
            Logger.Info($"[{ProcessId.Port}]: Message added - [{e.Type}]");
        }

        public void SendMessageOverNetwork(ProtoComm.Message msg, string remoteHost, int remotePort)
        {
            byte[] serializedMsg = msg.ToByteArray();

            // TODO maybe handle here NetworkException
            _networkHandler.SendMessage(serializedMsg, remoteHost, remotePort);

            Logger.Debug($"[{ProcessId.Port}]: Message [{msg.NetworkMessage.Message}] sent to [{remoteHost}:{remotePort}]");       // TODO refactor here
        }

        private void MessageListenerFallback(Task antecedent)
        {
            if (antecedent.Status == TaskStatus.RanToCompletion)
            {
                Logger.Debug($"[{ProcessId.Port}]: MessageListener stopped");
                return;
            } 
            else if (antecedent.Status == TaskStatus.Faulted)
            { 
                var ex = antecedent.Exception?.GetBaseException();
                Logger.Fatal($"[{ProcessId.Port}]: {ex.Message}");
                Stop();     // TODO throw StopProgramException to stop whole program?
            }
        }

        private void RegisterToHub()
        {
            if (_wasStopped)
                return;

            var procRegistration = new ProtoComm.ProcRegistration
            {
                Owner = ProcessId.Owner,
                Index = ProcessId.Index
            };

            var wrapperMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.ProcRegistration,
                ProcRegistration = procRegistration,
                //SystemId = "sys-1",     // TODO should be added?!
                //ToAbstractionId = "app",
                MessageUuid = Guid.NewGuid().ToString()
            };

            var networkMsg = new ProtoComm.NetworkMessage
            {
                Message = wrapperMsg,
                SenderHost = ProcessId.Host,
                SenderListeningPort = ProcessId.Port
            };

            var outMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.NetworkMessage,
                NetworkMessage = networkMsg,
                SystemId = wrapperMsg.SystemId,
                ToAbstractionId = wrapperMsg.ToAbstractionId,
                MessageUuid = Guid.NewGuid().ToString()
            };

            SendMessageOverNetwork(outMsg, HubProcessId.Host, HubProcessId.Port);
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

        protected virtual void OnMessageReceived(NetworkHandler p, ProtoComm.Message msg)
        {
            if (msg.NetworkMessage.Message.Type == ProtoComm.Message.Types.Type.ProcInitializeSystem)
            {
                var procInitSysMsg = msg.NetworkMessage.Message.ProcInitializeSystem;
                foreach (var proc in procInitSysMsg.Processes)
                {
                    if (!Processes.Add(proc))
                    {
                        Logger.Error($"[{ProcessId.Port}]: Could not add a process from ProcInitializeSystem");
                        // TODO should throw exception?
                    }
                }
            }
            else if (msg.NetworkMessage.Message.Type == ProtoComm.Message.Types.Type.ProcDestroySystem)
            {
                Processes.Clear();
                // TODO create separate queue for messages/events -> On ProcDestroy -> should clear that queue
            }
            else
            {
                AddToMessageQueue(msg);
                // TODO maybe process ProcInit + ProcDestroy also here?
            }
        }

        private void EventLoop()
        {
            if (_wasStopped)
                return;
            
            foreach (var msg in _messageQueue.GetConsumingEnumerable())
            {
                Logger.Warn($"[{ProcessId.Port}]: {msg.Type}");

                try
                {
                    HandleReceivedMessage(msg);
                }
                catch (Exception ex)        // TODO replace with more specific exception
                {
                    Logger.Error($"[{ProcessId.Port}]: {ex.Message}");
                    continue;
                }
            }

            Logger.Debug($"[{ProcessId.Port}]: EventLoop stopped");
        }

        // TODO message handling should be done in separate class - maybe use dict to map type of message to corresponding alg
        private void HandleReceivedMessage(ProtoComm.Message msg)
        {
            if (!_abstractions.ContainsKey(msg.ToAbstractionId))
            {
                Logger.Error($"[{ProcessId.Port}]: Abstractions dict does not contain - [{msg.ToAbstractionId}]");
                return;
            }

            if (!_abstractions[msg.ToAbstractionId].Handle(msg))
            {
                Logger.Error($"[{ProcessId.Port}]: Could not handle message: [{msg}]");
            }
        }
    }
}
