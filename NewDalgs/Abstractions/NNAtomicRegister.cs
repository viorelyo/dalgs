using NewDalgs.Utils;
using System;
using System.Collections.Concurrent;

namespace NewDalgs.Abstractions
{
    class NNAREntity
    {
        public int Timestamp { get; set; }
        public int WriterRank { get; set; }
        public ProtoComm.Value Value { get; set; }
    }

    class NNAtomicRegister : Abstraction
    {
        public static readonly string Name = "nnar";

        private NNAREntity _nnarEntity = new NNAREntity { Timestamp = 0, WriterRank = 0, Value = new ProtoComm.Value { Defined = false } };
        private int _acks = 0;
        private int _readId = 0;
        private ConcurrentDictionary<string, NNAREntity> _readList = new ConcurrentDictionary<string, NNAREntity>();
        private bool _isReading = false;

        private ProtoComm.Value _writeVal = new ProtoComm.Value { Defined = false };
        private ProtoComm.Value _readVal = new ProtoComm.Value { Defined = false };
        

        public NNAtomicRegister(string abstractionId, System.System system)
            : base(abstractionId, system)
        {
            _system.RegisterAbstraction(new PerfectLink(AbstractionIdUtil.GetChildAbstractionId(_abstractionId, PerfectLink.Name), _system));
            _system.RegisterAbstraction(new BestEffortBroadcast(AbstractionIdUtil.GetChildAbstractionId(_abstractionId, BestEffortBroadcast.Name), _system));
        }

        public override bool Handle(ProtoComm.Message msg)
        {
            if (msg.Type == ProtoComm.Message.Types.Type.NnarRead)
            {
                HandleNNarRead(msg.SystemId);
                return true;
            }

            if (msg.Type == ProtoComm.Message.Types.Type.NnarWrite)
            {
                HandleNNarWrite(msg);
                return true;
            }

            if (msg.Type == ProtoComm.Message.Types.Type.BebDeliver)
            {
                var bebDeliverMsg = msg.BebDeliver;
                if (bebDeliverMsg.Message.Type == ProtoComm.Message.Types.Type.NnarInternalRead)
                {
                    HandleNNarInternalRead(bebDeliverMsg);
                    return true;
                }

                if (bebDeliverMsg.Message.Type == ProtoComm.Message.Types.Type.NnarInternalWrite)
                {
                    HandleNNarInternalWrite(bebDeliverMsg);
                    return true;
                }

                return false;
            }

            if (msg.Type == ProtoComm.Message.Types.Type.PlDeliver)
            {
                var plDeliverMsg = msg.PlDeliver;
                if (plDeliverMsg.Message.Type == ProtoComm.Message.Types.Type.NnarInternalValue)
                {
                    HandleNNarInternalValue(plDeliverMsg);
                    return true;
                }

                if (plDeliverMsg.Message.Type == ProtoComm.Message.Types.Type.NnarInternalAck)
                {
                    HandleNNarInternalAck(plDeliverMsg);
                    return true;
                }

                return false;
            }

            return false;
        }

        private void HandleNNarInternalAck(ProtoComm.PlDeliver plDeliverMsg)
        {
            var nnarInternalAckMsg = plDeliverMsg.Message.NnarInternalAck;
            if (nnarInternalAckMsg.ReadId != _readId)
            {
                return;     // TODO check if should do anything else
            }

            _acks++;
            if (_acks > (_system.Processes.Count / 2))
            {
                _acks = 0;
                if (_isReading)
                {
                    _isReading = false;

                    var outMsg = new ProtoComm.Message
                    {
                        Type = ProtoComm.Message.Types.Type.NnarReadReturn,
                        NnarReadReturn = new ProtoComm.NnarReadReturn
                        {
                            Value = _readVal
                        },
                        SystemId = plDeliverMsg.Message.SystemId,
                        ToAbstractionId = AbstractionIdUtil.GetParentAbstractionId(_abstractionId),
                        FromAbstractionId = _abstractionId,
                        MessageUuid = Guid.NewGuid().ToString()
                    };

                    _system.AddToMessageQueue(outMsg);
                }
                else
                {
                    var outMsg = new ProtoComm.Message
                    {
                        Type = ProtoComm.Message.Types.Type.NnarWriteReturn,
                        NnarWriteReturn = new ProtoComm.NnarWriteReturn(),
                        SystemId = plDeliverMsg.Message.SystemId,
                        ToAbstractionId = AbstractionIdUtil.GetParentAbstractionId(_abstractionId),
                        FromAbstractionId = _abstractionId,
                        MessageUuid = Guid.NewGuid().ToString()
                    };

                    _system.AddToMessageQueue(outMsg);
                }
            }
        }

        private void HandleNNarInternalValue(ProtoComm.PlDeliver plDeliverMsg)
        {
            throw new NotImplementedException();
        }

        private void HandleNNarInternalWrite(ProtoComm.BebDeliver bebDeliverMsg)
        {
            var nnarInternalWriteMsg = bebDeliverMsg.Message.NnarInternalWrite;

            var receivedNNAREntity = new NNAREntity
            {
                Timestamp = nnarInternalWriteMsg.Timestamp,
                WriterRank = nnarInternalWriteMsg.WriterRank,
                Value = nnarInternalWriteMsg.Value
            };

            if ((_nnarEntity.Timestamp > receivedNNAREntity.Timestamp) || 
                ((_nnarEntity.Timestamp == receivedNNAREntity.Timestamp) && (_nnarEntity.WriterRank > receivedNNAREntity.WriterRank)))
            {
                _nnarEntity = receivedNNAREntity;
            }

            var plSendMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.NnarInternalAck,
                NnarInternalAck = new ProtoComm.NnarInternalAck
                {
                    ReadId = _readId
                },
                SystemId = bebDeliverMsg.Message.SystemId,
                ToAbstractionId = _abstractionId,
                FromAbstractionId = _abstractionId,
                MessageUuid = Guid.NewGuid().ToString()
            };

            var msgOut = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.PlSend,
                PlSend = new ProtoComm.PlSend
                {
                    Message = plSendMsg,
                    Destination = bebDeliverMsg.Sender
                },
                SystemId = bebDeliverMsg.Message.SystemId,
                ToAbstractionId = AbstractionIdUtil.GetChildAbstractionId(_abstractionId, PerfectLink.Name),
                FromAbstractionId = _abstractionId,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _system.AddToMessageQueue(msgOut);
        }

        private void HandleNNarInternalRead(ProtoComm.BebDeliver bebDeliverMsg)
        {
            throw new NotImplementedException();
        }

        private void HandleNNarRead(string systemId)
        {
            _readId++;
            _acks = 0;
            _readList = new ConcurrentDictionary<string, NNAREntity>();
            _isReading = true;

            var nnarInternalReadMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.NnarInternalRead,
                NnarInternalRead = new ProtoComm.NnarInternalRead
                {
                    ReadId = _readId
                },
                SystemId = systemId,
                ToAbstractionId = _abstractionId,
                FromAbstractionId = _abstractionId,
                MessageUuid = Guid.NewGuid().ToString()
            };

            var msgOut = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.BebBroadcast,
                BebBroadcast = new ProtoComm.BebBroadcast
                {
                    Message = nnarInternalReadMsg
                },
                SystemId = systemId,
                ToAbstractionId = AbstractionIdUtil.GetChildAbstractionId(_abstractionId, BestEffortBroadcast.Name),
                FromAbstractionId = _abstractionId,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _system.AddToMessageQueue(msgOut);
        }

        private void HandleNNarWrite(ProtoComm.Message msg)
        {
            var nnarWriteMsg = msg.NnarWrite;

            _readId++;
            _writeVal = new ProtoComm.Value { Defined = true, V = nnarWriteMsg.Value.V };
            _acks = 0;
            _readList = new ConcurrentDictionary<string, NNAREntity>();

            var nnarInternalReadMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.NnarInternalRead,
                NnarInternalRead = new ProtoComm.NnarInternalRead
                {
                    ReadId = _readId
                },
                SystemId = msg.SystemId,
                ToAbstractionId = _abstractionId,
                FromAbstractionId = _abstractionId,
                MessageUuid = Guid.NewGuid().ToString()
            };

            var msgOut = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.BebBroadcast,
                BebBroadcast = new ProtoComm.BebBroadcast
                {
                    Message = nnarInternalReadMsg
                },
                SystemId = msg.SystemId,
                ToAbstractionId = AbstractionIdUtil.GetChildAbstractionId(_abstractionId, BestEffortBroadcast.Name),
                FromAbstractionId = _abstractionId,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _system.AddToMessageQueue(msgOut);
        }
    }
}
