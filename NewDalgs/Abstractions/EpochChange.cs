using NewDalgs.Utils;
using System;

namespace NewDalgs.Abstractions
{
    class EpochChange : Abstraction
    {
        public static readonly string Name = "ec";

        private ProtoComm.ProcessId _trusted;
        private int _lastTimestamp = 0;
        private int _timestamp;

        public EpochChange(string abstractionId, System.System system)
            : base(abstractionId, system)
        {
            _system.RegisterAbstraction(new PerfectLink(AbstractionIdUtil.GetChildAbstractionId(_abstractionId, PerfectLink.Name), _system));
            _system.RegisterAbstraction(new BestEffortBroadcast(AbstractionIdUtil.GetChildAbstractionId(_abstractionId, BestEffortBroadcast.Name), _system));
            _system.RegisterAbstraction(new EventualLeaderDetector(AbstractionIdUtil.GetChildAbstractionId(_abstractionId, EventualLeaderDetector.Name), _system));

            _trusted = ProcessIdUtil.FindMaxRank(_system.Processes);
            _timestamp = _system.ProcessId.Rank;
        }

        public override bool Handle(ProtoComm.Message msg)
        {
            if (msg.Type == ProtoComm.Message.Types.Type.EldTrust)
            {
                HandleEldTrust(msg);
                return true;
            }

            if (msg.Type == ProtoComm.Message.Types.Type.BebDeliver)
            {
                if (msg.BebDeliver.Message.Type == ProtoComm.Message.Types.Type.EcInternalNewEpoch)
                {
                    HandleNewEpoch(msg);
                    return true;
                }

                return false;
            }

            if (msg.Type == ProtoComm.Message.Types.Type.PlDeliver)
            {
                if (msg.Type == ProtoComm.Message.Types.Type.EcInternalNack)
                {
                    HandleNack();
                    return true;
                }

                return false;
            }

            return false;
        }

        private void HandleNack()
        {
            if (_trusted.Equals(_system.ProcessId))
            {
                _timestamp += _system.Processes.Count;

                var outMsg = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.BebBroadcast,
                    BebBroadcast = new ProtoComm.BebBroadcast
                    {
                        Message = new ProtoComm.Message
                        {
                            Type = ProtoComm.Message.Types.Type.EcInternalNewEpoch,
                            EcInternalNewEpoch = new ProtoComm.EcInternalNewEpoch
                            {
                                Timestamp = _timestamp
                            },
                            SystemId = _system.SystemId,
                            ToAbstractionId = _abstractionId,
                            FromAbstractionId = _abstractionId,
                            MessageUuid = Guid.NewGuid().ToString()
                        }
                    },
                    SystemId = _system.SystemId,
                    ToAbstractionId = AbstractionIdUtil.GetChildAbstractionId(_abstractionId, BestEffortBroadcast.Name),
                    FromAbstractionId = _abstractionId,
                    MessageUuid = Guid.NewGuid().ToString()
                };

                _system.TriggerEvent(outMsg);
            }
        }

        private void HandleNewEpoch(ProtoComm.Message msg)
        {
            var sender = msg.BebDeliver.Sender;
            var newEpochMsg = msg.BebDeliver.Message.EcInternalNewEpoch;
            var newTimestamp = newEpochMsg.Timestamp;

            if (sender.Equals(_trusted) && (newTimestamp > _lastTimestamp))
            {
                _lastTimestamp = newTimestamp;

                var outMsg = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.EcStartEpoch,
                    EcStartEpoch = new ProtoComm.EcStartEpoch
                    {
                        NewLeader = sender,
                        NewTimestamp = newTimestamp
                    },
                    SystemId = _system.SystemId,
                    ToAbstractionId = AbstractionIdUtil.GetParentAbstractionId(_abstractionId),
                    FromAbstractionId = _abstractionId,
                    MessageUuid = Guid.NewGuid().ToString()
                };

                _system.TriggerEvent(outMsg);
            }
            else
            {
                var outMsg = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.PlSend,
                    PlSend = new ProtoComm.PlSend
                    {
                        Destination = sender,
                        Message = new ProtoComm.Message
                        {
                            Type = ProtoComm.Message.Types.Type.EcInternalNack,
                            EcInternalNack = new ProtoComm.EcInternalNack(),
                            SystemId = _system.SystemId,
                            ToAbstractionId = _abstractionId,
                            FromAbstractionId = _abstractionId,
                            MessageUuid = Guid.NewGuid().ToString()
                        }
                    },
                    SystemId = _system.SystemId,
                    ToAbstractionId = AbstractionIdUtil.GetChildAbstractionId(_abstractionId, PerfectLink.Name),
                    FromAbstractionId = _abstractionId,
                    MessageUuid = Guid.NewGuid().ToString()
                };

                _system.TriggerEvent(outMsg);
            }
        }

        private void HandleEldTrust(ProtoComm.Message msg)
        {
            _trusted = msg.EldTrust.Process;

            if (_trusted.Equals(_system.ProcessId))
            {
                _timestamp += _system.Processes.Count;

                var outMsg = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.BebBroadcast,
                    BebBroadcast = new ProtoComm.BebBroadcast
                    {
                        Message = new ProtoComm.Message
                        {
                            Type = ProtoComm.Message.Types.Type.EcInternalNewEpoch,
                            EcInternalNewEpoch = new ProtoComm.EcInternalNewEpoch
                            {
                                Timestamp = _timestamp
                            },
                            SystemId = _system.SystemId,
                            ToAbstractionId = _abstractionId,
                            FromAbstractionId = _abstractionId,
                            MessageUuid = Guid.NewGuid().ToString()
                        }
                    },
                    SystemId = _system.SystemId,
                    ToAbstractionId = AbstractionIdUtil.GetChildAbstractionId(_abstractionId, BestEffortBroadcast.Name),
                    FromAbstractionId = _abstractionId,
                    MessageUuid = Guid.NewGuid().ToString()
                };

                _system.TriggerEvent(outMsg);
            }
        }
    }
}
