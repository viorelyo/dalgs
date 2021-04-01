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

        private bool _isRunning;

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
            _isRunning = true;

            SubscribeToMessageListener();
            _messageListener = new Task(() =>
                {
                    _networkHandler.ListenForConnections();
                });
            //_messageListener.ContinueWith(MessageListenerExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
            _messageListener.Start();

            try
            {
                RegisterToHub();
            }
            catch (NetworkException ex)
            {
                Logger.Fatal(ex);
                Stop();
                // TODO throw StopProgramException to stop whole program?
                return;
            }

            Logger.Info($"[{_processId.Port}]: Process registered - [{_processId.Owner}-{_processId.Index}]");

            var _eventLoopTask = Task.Run(() => this.EventLoop());
            
            try
            {
                _messageListener.Wait();
            }
            catch (AggregateException ex)
            {
                var exception = ex.GetBaseException();
                Logger.Fatal(exception);

                Stop();
            }

            _eventLoopTask.Wait();
        }

        public void Stop()
        {
            if (!_isRunning)
                return;

            UnsubscribeFromMessageListener();
            _networkHandler.StopListener();     // TODO would be nice to notify dalgs that process is unregistered

            // TODO try the alternative with continue when onlyfualt => try except aggregate + get rid of task for eventLoop

            _isRunning = false;
            Logger.Warn($"[{_processId.Port}]: isRunning: {_isRunning}");      // TODO
        }

        private void MessageListenerExceptionHandler(Task task)
        {
            var exception = task.Exception?.GetBaseException();
            Logger.Fatal(exception);
            Stop();
            // TODO throw StopProgramException to stop whole program?
        }

        private void RegisterToHub()
        {
            if (!_isRunning)
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
            Logger.Warn($"[{_processId.Port}]: Unsubscribed");      // TODO
        }

        protected virtual void OnMessageReceived(NetworkHandler p, ReceivedMessage e)
        {
            _messageQueue.Add(e);
            Logger.Info($"[{_processId.Port}]: Message added - [{e.Message.Type}]");
        }

        private void EventLoop()
        {
            if (!_isRunning)
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
                    Logger.Error(ex);
                    continue;
                }
            }

            Logger.Warn($"[{_processId.Port}]: EventLoop stopped");     // TODO
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
