﻿using NewDalgs.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace NewDalgs.Abstractions
{
    class EventuallyPerfectFailureDetector : Abstraction
    {
        public static readonly string Name = "epfd";

        private static readonly int Delta = 100;    // 100 milliseconds

        private HashSet<ProtoComm.ProcessId> _alive;
        private HashSet<ProtoComm.ProcessId> _suspected = new HashSet<ProtoComm.ProcessId>();
        private int _delay = Delta;
        
        public EventuallyPerfectFailureDetector(string abstractionId, System.System system)
            : base(abstractionId, system)
        {
            _system.RegisterAbstraction(new PerfectLink(AbstractionIdUtil.GetChildAbstractionId(_abstractionId, PerfectLink.Name), _system));

            _alive = new HashSet<ProtoComm.ProcessId>(_system.Processes);
            StartTimer();
        }

        public override bool Handle(ProtoComm.Message msg)
        {
            if (msg.Type == ProtoComm.Message.Types.Type.EpfdTimeout)
            {
                HandleEpfdTimeout();
                return true;
            }

            if (msg.Type == ProtoComm.Message.Types.Type.PlDeliver)
            {
                if (msg.PlDeliver.Message.Type == ProtoComm.Message.Types.Type.EpfdInternalHeartbeatRequest)
                {
                    HandleHeartbeatRequest(msg);
                    return true;
                }

                if (msg.PlDeliver.Message.Type == ProtoComm.Message.Types.Type.EpfdInternalHeartbeatReply)
                {
                    HandleHeartbeatReply(msg);
                    return true;
                }

                return false;
            }            

            return false;
        }

        private void HandleHeartbeatReply(ProtoComm.Message msg)
        {
            var senderProc = msg.PlDeliver.Sender;

            _alive.Add(senderProc);
        }

        private void HandleHeartbeatRequest(ProtoComm.Message msg)
        {
            var senderProc = msg.PlDeliver.Sender;

            var sendMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.PlSend,
                PlSend = new ProtoComm.PlSend
                {
                    Destination = senderProc,
                    Message = new ProtoComm.Message
                    {
                        Type = ProtoComm.Message.Types.Type.EpfdInternalHeartbeatReply,
                        EpfdInternalHeartbeatReply = new ProtoComm.EpfdInternalHeartbeatReply(),
                        SystemId = "sys-1",    // TODO sysid should be globally available :(
                        ToAbstractionId = _abstractionId,    // TODO check this
                        FromAbstractionId = _abstractionId,
                        MessageUuid = Guid.NewGuid().ToString()
                    }
                },
                SystemId = "sys-1",    // TODO sysid should be globally available :(
                ToAbstractionId = AbstractionIdUtil.GetChildAbstractionId(_abstractionId, PerfectLink.Name),    // TODO check this
                FromAbstractionId = _abstractionId,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _system.TriggerEvent(sendMsg);
        }

        private void HandleEpfdTimeout()
        {
            if (_alive.Intersect(_suspected).Count() != 0)
            {
                _delay += Delta;
            }
            
            foreach (var procId in _system.Processes)
            {
                var msg = new ProtoComm.Message
                {
                    SystemId = "sys-1",    // TODO sysid should be globally available :(
                    ToAbstractionId = AbstractionIdUtil.GetParentAbstractionId(_abstractionId),    // TODO check this
                    FromAbstractionId = _abstractionId,
                    MessageUuid = Guid.NewGuid().ToString()
                };

                if (!_alive.Contains(procId) && !_suspected.Contains(procId))
                {
                    _suspected.Add(procId);

                    msg.Type = ProtoComm.Message.Types.Type.EpfdSuspect;
                    msg.EpfdSuspect = new ProtoComm.EpfdSuspect
                    {
                        Process = procId
                    };
                        
                    _system.TriggerEvent(msg);
                }
                else if (_alive.Contains(procId) && _suspected.Contains(procId))
                {
                    _suspected.Remove(procId);

                    msg.Type = ProtoComm.Message.Types.Type.EpfdRestore;
                    msg.EpfdRestore = new ProtoComm.EpfdRestore
                    {
                        Process = procId
                    };

                    _system.TriggerEvent(msg);
                }

                var sendMsg = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.PlSend,
                    PlSend = new ProtoComm.PlSend
                    {
                        Destination = procId,
                        Message = new ProtoComm.Message
                        {
                            Type = ProtoComm.Message.Types.Type.EpfdInternalHeartbeatRequest,
                            EpfdInternalHeartbeatRequest = new ProtoComm.EpfdInternalHeartbeatRequest(),
                            SystemId = "sys-1",    // TODO sysid should be globally available :(
                            ToAbstractionId = _abstractionId,    // TODO check this
                            FromAbstractionId = _abstractionId,
                            MessageUuid = Guid.NewGuid().ToString()
                        }
                    },
                    SystemId = "sys-1",    // TODO sysid should be globally available :(
                    ToAbstractionId = AbstractionIdUtil.GetChildAbstractionId(_abstractionId, PerfectLink.Name),    // TODO check this
                    FromAbstractionId = _abstractionId,
                    MessageUuid = Guid.NewGuid().ToString()
                };

                _system.TriggerEvent(sendMsg);
            }

            _alive.Clear();
            StartTimer();
        }

        private void StartTimer()
        {
            // TODO check if the timer should be reset first
            var timer = new Timer(_delay);
            timer.Elapsed += new ElapsedEventHandler(
                (source, e) => 
                {
                    var msg = new ProtoComm.Message
                    {
                        Type = ProtoComm.Message.Types.Type.EpfdTimeout,
                        EpfdTimeout = new ProtoComm.EpfdTimeout(),
                        SystemId = "sys-1",    // TODO sysid should be globally available :(
                        ToAbstractionId = _abstractionId,    // TODO check this
                        FromAbstractionId = _abstractionId,
                        MessageUuid = Guid.NewGuid().ToString()
                    };

                    _system.TriggerEvent(msg);
                });
            timer.Start();
        }
    }
}
