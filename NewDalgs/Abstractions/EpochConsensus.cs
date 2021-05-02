using NewDalgs.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace NewDalgs.Abstractions
{
    class EpochConsensusState
    {
        public int ValTimestamp;
        public ProtoComm.Value Val;
    }

    class EpochConsensus : Abstraction
    {
        public static readonly string Name = "ep";

        private bool _halted = false;

        private int _epochTimestamp;
        private EpochConsensusState _state;
        private ProtoComm.Value _tmpVal = new ProtoComm.Value { Defined = false };
        private Dictionary<ProtoComm.ProcessId, EpochConsensusState> _states = new Dictionary<ProtoComm.ProcessId, EpochConsensusState>();
        private int _accepted = 0;

        // TODO decide if Leader should be included in constructor, even if it is not used
        public EpochConsensus(string abstractionId, System.System system, EpochConsensusState state, int epochTimestamp)
            : base(abstractionId, system)
        {
            _system.RegisterAbstraction(new PerfectLink(AbstractionIdUtil.GetChildAbstractionId(_abstractionId, PerfectLink.Name), _system));
            _system.RegisterAbstraction(new BestEffortBroadcast(AbstractionIdUtil.GetChildAbstractionId(_abstractionId, BestEffortBroadcast.Name), _system));       // TODO well...

            _epochTimestamp = epochTimestamp;
            _state = state;
        }

        public override bool Handle(ProtoComm.Message msg)
        {
            if (_halted)
                return false;

            if (msg.Type == ProtoComm.Message.Types.Type.EpPropose)
            {
                HandleEpPropose(msg);
                return true;
            }

            if (msg.Type == ProtoComm.Message.Types.Type.BebDeliver)
            {
                if (msg.BebDeliver.Message.Type == ProtoComm.Message.Types.Type.EpInternalRead)
                {
                    HandleEpRead(msg);
                    return true;
                }

                if (msg.BebDeliver.Message.Type == ProtoComm.Message.Types.Type.EpInternalWrite)
                {
                    HandleEpWrite(msg);
                    return true;
                }

                if (msg.BebDeliver.Message.Type == ProtoComm.Message.Types.Type.EpInternalDecided)
                {
                    HandleEpDecided(msg);
                    return true;
                }

                return false;
            }

            if (msg.Type == ProtoComm.Message.Types.Type.PlDeliver)
            {
                if (msg.BebDeliver.Message.Type == ProtoComm.Message.Types.Type.EpInternalState)
                {
                    HandleEpState(msg);
                    return true;
                }

                if (msg.BebDeliver.Message.Type == ProtoComm.Message.Types.Type.EpInternalAccept)
                {
                    HandleEpAccept();
                    return true;
                }

                return false;
            }

            if (msg.Type == ProtoComm.Message.Types.Type.EpAbort)
            {
                HandleEpAbort();
                return true;
            }

            return false;
        }

        private void HandleEpAbort()
        {
            // TODO check this
            var outMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.EpAborted,
                EpAborted = new ProtoComm.EpAborted
                {
                    Ets = _epochTimestamp,
                    Value = _state.Val,
                    ValueTimestamp = _state.ValTimestamp
                },
                SystemId = "sys-1",    // TODO sysid should be globally available :(
                ToAbstractionId = AbstractionIdUtil.GetChildAbstractionId(_abstractionId, BestEffortBroadcast.Name),    // TODO check this
                FromAbstractionId = _abstractionId,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _system.TriggerEvent(outMsg);

            _halted = true;
        }

        private void HandleEpDecided(ProtoComm.Message msg)
        {
            // TODO check this
            var decidedMsg = msg.BebDeliver.Message.EpInternalDecided;

            var outMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.EpDecide,
                EpDecide = new ProtoComm.EpDecide
                {
                    Ets = _epochTimestamp,
                    Value = decidedMsg.Value
                },
                SystemId = "sys-1",    // TODO sysid should be globally available :(
                ToAbstractionId = AbstractionIdUtil.GetParentAbstractionId(_abstractionId),    // TODO check this
                FromAbstractionId = _abstractionId,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _system.TriggerEvent(outMsg);
        }

        private void HandleEpAccept()
        {
            _accepted += 1;

            HandleAcceptedCheck();
        }

        private void HandleAcceptedCheck()
        {
            if (_accepted > (_system.Processes.Count / 2))
            {
                _accepted = 0;

                var outMsg = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.BebBroadcast,
                    BebBroadcast = new ProtoComm.BebBroadcast
                    {
                        Message = new ProtoComm.Message
                        {
                            Type = ProtoComm.Message.Types.Type.EpInternalDecided,
                            EpInternalDecided = new ProtoComm.EpInternalDecided
                            {
                                Value = _tmpVal
                            },
                            SystemId = "sys-1",    // TODO check this
                            ToAbstractionId = _abstractionId,   // TODO check this
                            FromAbstractionId = _abstractionId,
                            MessageUuid = Guid.NewGuid().ToString()
                        }
                    },
                    SystemId = "sys-1",    // TODO sysid should be globally available :(
                    ToAbstractionId = AbstractionIdUtil.GetChildAbstractionId(_abstractionId, BestEffortBroadcast.Name),    // TODO check this
                    FromAbstractionId = _abstractionId,
                    MessageUuid = Guid.NewGuid().ToString()
                };

                _system.TriggerEvent(outMsg);
            }
        }

        private void HandleEpWrite(ProtoComm.Message msg)
        {
            var sender = msg.BebDeliver.Sender;
            var msgWrite = msg.BebDeliver.Message.EpInternalWrite;

            _state = new EpochConsensusState
            {
                ValTimestamp = _epochTimestamp,
                Val = msgWrite.Value
            };

            var outMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.PlSend,
                PlSend = new ProtoComm.PlSend
                {
                    Destination = sender,
                    Message = new ProtoComm.Message
                    {
                        Type = ProtoComm.Message.Types.Type.EpInternalAccept,
                        EpInternalAccept = new ProtoComm.EpInternalAccept(),
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

            _system.TriggerEvent(outMsg);
        }

        private void HandleEpState(ProtoComm.Message msg)
        {
            var sender = msg.PlDeliver.Sender;
            var stateMsg = msg.PlDeliver.Message.EpInternalState;

            _states[sender] = new EpochConsensusState
            {
                Val = stateMsg.Value,
                ValTimestamp = stateMsg.ValueTimestamp
            };

            HandleStatesCheck();
        }

        private void HandleStatesCheck()
        {
            if (_states.Count > (_system.Processes.Count / 2))
            {
                var highest = EpochConsensusStateUtil.FindHighest(_states.Values);
                if (highest == null)
                    return;

                if (highest.Val != null)
                {
                    _tmpVal = highest.Val;
                }

                _states.Clear();

                var outMsg = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.BebBroadcast,
                    BebBroadcast = new ProtoComm.BebBroadcast
                    {
                        Message = new ProtoComm.Message
                        {
                            Type = ProtoComm.Message.Types.Type.EpInternalWrite,
                            EpInternalWrite = new ProtoComm.EpInternalWrite
                            {
                                Value = _tmpVal
                            },
                            SystemId = "sys-1",    // TODO check this
                            ToAbstractionId = _abstractionId,   // TODO check this
                            FromAbstractionId = _abstractionId,
                            MessageUuid = Guid.NewGuid().ToString()
                        }
                    },
                    SystemId = "sys-1",    // TODO sysid should be globally available :(
                    ToAbstractionId = AbstractionIdUtil.GetChildAbstractionId(_abstractionId, BestEffortBroadcast.Name),    // TODO check this
                    FromAbstractionId = _abstractionId,
                    MessageUuid = Guid.NewGuid().ToString()
                };

                _system.TriggerEvent(outMsg);
            }
        }

        private void HandleEpRead(ProtoComm.Message msg)
        {
            var sender = msg.BebDeliver.Sender;

            var outMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.PlSend,
                PlSend = new ProtoComm.PlSend
                {
                    Destination = sender,
                    Message = new ProtoComm.Message
                    {
                        Type = ProtoComm.Message.Types.Type.EpInternalState,
                        EpInternalState = new ProtoComm.EpInternalState
                        {
                            Value = _state.Val,
                            ValueTimestamp = _state.ValTimestamp
                        },
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

            _system.TriggerEvent(outMsg);
        }

        private void HandleEpPropose(ProtoComm.Message msg)
        {
            _tmpVal = msg.EpPropose.Value;

            var outMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.BebBroadcast,
                BebBroadcast = new ProtoComm.BebBroadcast
                {
                    Message = new ProtoComm.Message
                    {
                        Type = ProtoComm.Message.Types.Type.EpInternalRead,
                        EpInternalRead = new ProtoComm.EpInternalRead(),
                        SystemId = "sys-1",    // TODO check this
                        ToAbstractionId = _abstractionId,   // TODO check this
                        FromAbstractionId = _abstractionId,
                        MessageUuid = Guid.NewGuid().ToString()
                    }
                },
                SystemId = "sys-1",    // TODO sysid should be globally available :(
                ToAbstractionId = AbstractionIdUtil.GetChildAbstractionId(_abstractionId, BestEffortBroadcast.Name),    // TODO check this
                FromAbstractionId = _abstractionId,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _system.TriggerEvent(outMsg);
        }
    }
}
