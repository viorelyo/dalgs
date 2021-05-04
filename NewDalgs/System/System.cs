using Google.Protobuf;
using NewDalgs.Abstractions;
using NewDalgs.Networking;
using NewDalgs.Utils;
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

        private BlockingCollection<ProtoComm.Message> _eventQueue;

        private bool _wasStopped = false;

        public System(ProtoComm.ProcessId processId, ProtoComm.ProcessId hubProcesId)
        {
            ProcessId = processId;
            HubProcessId = hubProcesId;

            Processes = new HashSet<ProtoComm.ProcessId>();

            _abstractions = new ConcurrentDictionary<string, Abstraction>();

            _networkHandler = new NetworkHandler(processId.Host, processId.Port);
            _eventQueue = new BlockingCollection<ProtoComm.Message>(new ConcurrentQueue<ProtoComm.Message>());
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

            RegisterToHub();    
    
            EventLoop();

            messageListenerFallbackTask.Wait();
        }

        public void Stop()
        {
            if (_wasStopped)
                return;

            UnsubscribeFromMessageListener();
            _networkHandler.StopListener();     // Would be nice to notify dalgs that process is unregistered

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
            if (_abstractions.TryAdd(abstraction.GetId(), abstraction))
            {
                Logger.Trace($"[{ProcessId.Port}]: New abstraction registered: {abstraction.GetId()}");
            }
        }

        /// <summary>
        /// Adds event (ProtoComm.Message) to eventQueue
        /// </summary>
        public void TriggerEvent(ProtoComm.Message e)
        {
            try
            {
                _eventQueue.Add(e);
            }
            catch (Exception ex)
            {
                Logger.Error($"[{ProcessId.Port}]: {ex.Message}. Exception occurred while triggering event - [{e}]");
            }
        }

        private void ClearEventQueue()
        {
            if (_eventQueue == null)
            {
                return;
            }

            while (_eventQueue.Count > 0)
            {
                _eventQueue.TryTake(out var _);
            }
        }

        public bool SendMessageOverNetwork(ProtoComm.Message msg, string remoteHost, int remotePort)
        {
            // Each travelling message should be wrapped in Message(NetworkMessage)
            if (msg.Type != ProtoComm.Message.Types.Type.NetworkMessage)
            {
                Logger.Error($"[{ProcessId.Port}]: Invalid message sent - {msg}");
                return false;
            }

            var innerMsg = msg.NetworkMessage.Message;
            Logger.Info($"[{ProcessId.Port}]: ===> Sending message: [{innerMsg.Type}] -> [{innerMsg.ToAbstractionId}] ({remoteHost}:{remotePort})");

            byte[] serializedMsg = msg.ToByteArray();

            try
            {
                _networkHandler.SendMessage(serializedMsg, remoteHost, remotePort);
                return true;
            }
            catch (NetworkException ex)
            {
                Logger.Error($"[{ProcessId.Port}]: {ex.Message}");
                return false;
            }
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
                Stop();
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
                ToAbstractionId = HubProcessId.Owner,
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

            if (SendMessageOverNetwork(outMsg, HubProcessId.Host, HubProcessId.Port))
            {
                Logger.Info($"[{ProcessId.Port}]: Process registered - [{ProcessId.Owner}-{ProcessId.Index}]");
            }
            else
            {
                Logger.Fatal($"[{ProcessId.Port}]: Could not register to Hub");
                Stop();
            }
        }

        private void SubscribeToMessageListener()
        {
            _networkHandler.OnPublish += OnMessageReceived;
        }

        private void UnsubscribeFromMessageListener()
        {
            _networkHandler.OnPublish -= OnMessageReceived;
            _eventQueue.CompleteAdding();
        }

        protected virtual void OnMessageReceived(NetworkHandler p, byte[] serializedMsg)
        {
            ProtoComm.Message msg;
            try
            {
                msg = ProtoComm.Message.Parser.ParseFrom(serializedMsg);
            }
            catch (InvalidProtocolBufferException)
            {
                Logger.Error($"[{ProcessId.Port}]: Protobuf could not parse incoming serialized message. Message ignored");
                return;
            }

            // Each incoming message should be wrapped into Message(NetworkMessage)
            if (msg.Type != ProtoComm.Message.Types.Type.NetworkMessage)
            {
                Logger.Error($"[{ProcessId.Port}]: Invalid message received - [{msg.Type}]. Message ignored");
                return;
            }

            var innerMsg = msg.NetworkMessage.Message;
            Logger.Info($"[{ProcessId.Port}]: <=== Message received: [{innerMsg.Type}] -> [{innerMsg.ToAbstractionId}]");

            if (innerMsg.Type == ProtoComm.Message.Types.Type.ProcInitializeSystem)
            {
                HandleProcInit(innerMsg);
            }
            else if (innerMsg.Type == ProtoComm.Message.Types.Type.ProcDestroySystem)
            {
                HandleProcDestroy(innerMsg);
            }
            else
            {
                TriggerEvent(msg);
            }
        }

        private void HandleProcDestroy(ProtoComm.Message innerMsg)
        {
            Processes.Clear();
            _abstractions.Clear();
        }

        private void HandleProcInit(ProtoComm.Message msg)
        {
            try
            {
                ClearEventQueue();

                RegisterAbstraction(new Application(Application.Name, this));

                var procInitSysMsg = msg.ProcInitializeSystem;
                foreach (var proc in procInitSysMsg.Processes)
                {
                    Processes.Add(proc);
                }

                var foundProcessId = FindProcessByHostAndPort(ProcessId.Host, ProcessId.Port);   // Hub updates info of ProcessId, so it should be replaced
                if (foundProcessId != null)
                {
                    ProcessId = foundProcessId;
                }
                else
                {
                    Logger.Fatal($"[{ProcessId.Port}]: Could not find ProcessId from ProcInitializeSystem");
                    Stop();
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal($"[{ProcessId.Port}]: Exception occurred while handling ProcInitializeSystem message - {ex.Message}");
                Stop();
            }
        }

        private void EventLoop()
        {
            if (_wasStopped)
                return;
            try
            {
                foreach (var msg in _eventQueue.GetConsumingEnumerable())
                {
                    try
                    {
                        HandleEvent(msg);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"[{ProcessId.Port}]: {ex.Message}. Exception occurred while handling message: [{msg}]");
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal($"[{ProcessId.Port}]: {ex.Message}. Exception occurred in EventLoop");
                Stop();
            }
            finally
            {
                Logger.Debug($"[{ProcessId.Port}]: EventLoop stopped");
            }
        }

        private void HandleEvent(ProtoComm.Message msg)
        {
            if (!_abstractions.ContainsKey(msg.ToAbstractionId))
            {
                HandleNewAbstractionId(msg);
            }

            if (!_abstractions[msg.ToAbstractionId].Handle(msg))
            {
                Logger.Error($"[{ProcessId.Port}]: Could not handle message: [{msg}]");
            }
        }

        private void HandleNewAbstractionId(ProtoComm.Message msg)
        {
            var nnarRegisterName = AbstractionIdUtil.GetNnarRegisterName(msg.ToAbstractionId);
            if (nnarRegisterName != "")
            {
                var nnarAbstractionId = AbstractionIdUtil.GetNnarAbstractionId(Application.Name, nnarRegisterName);
                RegisterAbstraction(new NNAtomicRegister(nnarAbstractionId, this));
                return;
            }

            var ucTopic = AbstractionIdUtil.GetUcTopicName(msg.ToAbstractionId);
            if (ucTopic != "")
            {
                this.TriggerEvent(msg);     // TODO check this
                Logger.Warn($"[{ProcessId.Port}] Message readded: [{msg}]");
                return;
            }

            Logger.Error($"[{ProcessId.Port}]: Could not identify ToAbstractionId: [{msg.ToAbstractionId}]");
        }
    }
}
