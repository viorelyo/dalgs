using NewDalgs.Utils;
using System;

namespace NewDalgs.Abstractions
{
    class UniformConsensus : Abstraction
    {
        public static readonly string Name = "uc";

        ProtoComm.Value _val = new ProtoComm.Value { Defined = false };
        bool _proposed = false;
        bool _decided = false;

        ProtoComm.ProcessId _leader;
        int _epochTimestamp = 0;

        ProtoComm.ProcessId _newLeader;
        int _newEpochTimestamp = 0;

        public UniformConsensus(string abstractionId, System.System system)
            : base(abstractionId, system)
        {
            _system.RegisterAbstraction(new EpochChange(AbstractionIdUtil.GetChildAbstractionId(_abstractionId, EpochChange.Name), _system));

            _leader = ProcessIdUtil.FindMaxRank(_system.Processes);

            EpochConsensusState ep0State = new EpochConsensusState
            {
                Val = new ProtoComm.Value { Defined = false },
                ValTimestamp = 0
            };

            _system.RegisterAbstraction(new EpochConsensus(AbstractionIdUtil.GetEpAbstractionId(_abstractionId, _epochTimestamp), _system, _leader, ep0State, _epochTimestamp));
        }

        public override bool Handle(ProtoComm.Message msg)
        {
            if (msg.Type == ProtoComm.Message.Types.Type.UcPropose)
            {
                HandleUcPropose(msg);
                return true;
            }

            if (msg.Type == ProtoComm.Message.Types.Type.EcStartEpoch)
            {
                HandleEcStartEpoch(msg);
                return true;
            }

            if (msg.Type == ProtoComm.Message.Types.Type.EpAborted)
            {
                var epAbortedMsg = msg.EpAborted;
                if (epAbortedMsg.Ets == _epochTimestamp)
                {
                    HandleEpAborted(msg);
                    return true;
                }
                return false;
            }

            if (msg.Type == ProtoComm.Message.Types.Type.EpDecide)
            {
                var epDecideMsg = msg.EpDecide;
                if (epDecideMsg.Ets == _epochTimestamp)
                {
                    HandleEpDecide(msg);
                    return true;
                }
                return false;
            }

            return false;
        }

        private void HandleEpDecide(ProtoComm.Message msg)
        {
            var epDecideMsg = msg.EpDecide;

            if (!_decided)
            {
                _decided = true;

                var outMsg = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.UcDecide,
                    UcDecide = new ProtoComm.UcDecide
                    {
                        Value = epDecideMsg.Value
                    },
                    SystemId = _system.SystemId,
                    ToAbstractionId = AbstractionIdUtil.GetParentAbstractionId(_abstractionId),
                    FromAbstractionId = _abstractionId,
                    MessageUuid = Guid.NewGuid().ToString()
                };

                _system.TriggerEvent(outMsg);
            }
        }

        private void HandleInternalCheck()
        {
            if (_system.ProcessId.Equals(_leader) && _val.Defined && !_proposed)
            {
                _proposed = true;

                var outMsg = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.EpPropose,
                    EpPropose = new ProtoComm.EpPropose
                    {
                        Value = _val
                    },
                    SystemId = _system.SystemId,
                    ToAbstractionId = AbstractionIdUtil.GetEpAbstractionId(_abstractionId, _epochTimestamp),
                    FromAbstractionId = _abstractionId,
                    MessageUuid = Guid.NewGuid().ToString()
                };

                _system.TriggerEvent(outMsg);
            }
        }

        private void HandleEpAborted(ProtoComm.Message msg)
        {
            var epAbortedMsg = msg.EpAborted;

            _epochTimestamp = _newEpochTimestamp;
            _leader = _newLeader;
            _proposed = false;

            EpochConsensusState epState = new EpochConsensusState
            {
                Val = epAbortedMsg.Value,
                ValTimestamp = epAbortedMsg.ValueTimestamp
            };

            _system.RegisterAbstraction(new EpochConsensus(AbstractionIdUtil.GetEpAbstractionId(_abstractionId, _epochTimestamp), _system, _leader, epState, _epochTimestamp));

            HandleInternalCheck();
        }

        private void HandleEcStartEpoch(ProtoComm.Message msg)
        {
            var ecStartEpochMsg = msg.EcStartEpoch;

            _newEpochTimestamp = ecStartEpochMsg.NewTimestamp;
            _newLeader = ecStartEpochMsg.NewLeader;

            var outMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.EpAbort,
                EpAbort = new ProtoComm.EpAbort(),
                SystemId = _system.SystemId,
                ToAbstractionId = AbstractionIdUtil.GetEpAbstractionId(_abstractionId, _epochTimestamp),
                FromAbstractionId = _abstractionId,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _system.TriggerEvent(outMsg);
        }

        private void HandleUcPropose(ProtoComm.Message msg)
        {
            _val = msg.UcPropose.Value;

            HandleInternalCheck();
        }
    }
}
