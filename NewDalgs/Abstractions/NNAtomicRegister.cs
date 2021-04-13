using NewDalgs.Utils;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace NewDalgs.Abstractions
{
    class NNAREntity : IComparable<NNAREntity>
    {
        public int Timestamp { get; set; }
        public int WriterRank { get; set; }
        public ProtoComm.Value Value { get; set; }

        public static bool operator >(NNAREntity n1, NNAREntity n2)
        {
            if ((n1.Timestamp > n2.Timestamp) ||
                ((n1.Timestamp == n2.Timestamp) && (n1.WriterRank > n2.WriterRank)))
            {
                return true;
            }

            return false;
        }

        public static bool operator <(NNAREntity n1, NNAREntity n2)
        {
            if ((n1.Timestamp < n2.Timestamp) ||
                ((n1.Timestamp == n2.Timestamp) && (n1.WriterRank < n2.WriterRank)))
            {
                return true;
            }

            return false;
        }

        public int CompareTo([AllowNull] NNAREntity other)
        {
            if (other == null)
                return 1;

            if (this > other)
            {
                return 1;
            }
            else if (this < other)
            {
                return -1;
            }

            return 0;
        }
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
                if (msg.BebDeliver.Message.Type == ProtoComm.Message.Types.Type.NnarInternalRead)
                {
                    HandleNNarInternalRead(msg);
                    return true;
                }

                if (msg.BebDeliver.Message.Type == ProtoComm.Message.Types.Type.NnarInternalWrite)
                {
                    HandleNNarInternalWrite(msg);
                    return true;
                }

                return false;
            }

            if (msg.Type == ProtoComm.Message.Types.Type.PlDeliver)
            {
                if (msg.PlDeliver.Message.Type == ProtoComm.Message.Types.Type.NnarInternalValue)
                {
                    HandleNNarInternalValue(msg);
                    return true;
                }

                if (msg.PlDeliver.Message.Type == ProtoComm.Message.Types.Type.NnarInternalAck)
                {
                    HandleNNarInternalAck(msg);
                    return true;
                }

                return false;
            }

            return false;
        }

        private NNAREntity GetHighest()
        {
            return _readList.Values.Max();
        }

        private void HandleNNarInternalAck(ProtoComm.Message msg)
        {
            var plDeliverMsg = msg.PlDeliver;
            var nnarInternalAckMsg = plDeliverMsg.Message.NnarInternalAck;
            if (nnarInternalAckMsg.ReadId != _readId)
            {
                return;
            }

            _acks++;
            if (_acks > (_system.Processes.Count / 2))
            {
                var outMsg = new ProtoComm.Message
                {
                    SystemId = msg.SystemId,
                    ToAbstractionId = AbstractionIdUtil.GetParentAbstractionId(_abstractionId),
                    FromAbstractionId = _abstractionId,
                    MessageUuid = Guid.NewGuid().ToString()
                };

                _acks = 0;
                if (_isReading)
                {
                    _isReading = false;

                    outMsg.Type = ProtoComm.Message.Types.Type.NnarReadReturn;
                    outMsg.NnarReadReturn = new ProtoComm.NnarReadReturn
                    {
                        Value = _readVal
                    };
                }
                else
                {
                    outMsg.Type = ProtoComm.Message.Types.Type.NnarWriteReturn;
                    outMsg.NnarWriteReturn = new ProtoComm.NnarWriteReturn();
                }

                _system.TriggerEvent(outMsg);
            }
        }

        private void HandleNNarInternalValue(ProtoComm.Message msg)
        {
            var plDeliverMsg = msg.PlDeliver;

            var nnarInternalValMsg = plDeliverMsg.Message.NnarInternalValue;
            if (nnarInternalValMsg.ReadId != _readId)
            {
                return;
            }

            var receivedNNAREntity = new NNAREntity
            {
                Timestamp = nnarInternalValMsg.Timestamp,
                WriterRank = nnarInternalValMsg.WriterRank,
                Value = nnarInternalValMsg.Value
            };

            _readList[plDeliverMsg.Sender.Owner + '-' + plDeliverMsg.Sender.Index] = receivedNNAREntity;

            if (_readList.Count > (_system.Processes.Count / 2))
            {
                var maxNnarEntity = GetHighest();
                _readVal = maxNnarEntity.Value;

                _readList.Clear();

                ProtoComm.NnarInternalWrite nnarInternalWriteMsg;
                if (_isReading)
                {
                    nnarInternalWriteMsg = new ProtoComm.NnarInternalWrite
                    {
                        ReadId = nnarInternalValMsg.ReadId,
                        Timestamp = maxNnarEntity.Timestamp,
                        WriterRank = maxNnarEntity.WriterRank,
                        Value = maxNnarEntity.Value
                    };
                }
                else
                {
                    nnarInternalWriteMsg = new ProtoComm.NnarInternalWrite
                    {
                        ReadId = nnarInternalValMsg.ReadId,
                        Timestamp = maxNnarEntity.Timestamp + 1,
                        WriterRank = _system.ProcessId.Rank,
                        Value = _writeVal
                    };
                }

                var outMsg = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.BebBroadcast,
                    BebBroadcast = new ProtoComm.BebBroadcast
                    {
                        Message = new ProtoComm.Message
                        {
                            Type = ProtoComm.Message.Types.Type.NnarInternalWrite,
                            NnarInternalWrite = nnarInternalWriteMsg,
                            SystemId = msg.SystemId,
                            ToAbstractionId = _abstractionId,
                            FromAbstractionId = _abstractionId,
                            MessageUuid = Guid.NewGuid().ToString()
                        }
                    },
                    SystemId = msg.SystemId,
                    ToAbstractionId = AbstractionIdUtil.GetChildAbstractionId(_abstractionId, BestEffortBroadcast.Name),
                    FromAbstractionId = _abstractionId,
                    MessageUuid = Guid.NewGuid().ToString()
                };

                _system.TriggerEvent(outMsg);
            }
        }

        private void HandleNNarInternalWrite(ProtoComm.Message msg)
        {
            var bebDeliverMsg = msg.BebDeliver;
            var nnarInternalWriteMsg = bebDeliverMsg.Message.NnarInternalWrite;

            var receivedNNAREntity = new NNAREntity
            {
                Timestamp = nnarInternalWriteMsg.Timestamp,
                WriterRank = nnarInternalWriteMsg.WriterRank,
                Value = nnarInternalWriteMsg.Value
            };

            if (_nnarEntity > receivedNNAREntity)
            {
                _nnarEntity = receivedNNAREntity;
            }

            var plSendMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.NnarInternalAck,
                NnarInternalAck = new ProtoComm.NnarInternalAck
                {
                    ReadId = nnarInternalWriteMsg.ReadId
                },
                SystemId = msg.SystemId,
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
                SystemId = msg.SystemId,
                ToAbstractionId = AbstractionIdUtil.GetChildAbstractionId(_abstractionId, PerfectLink.Name),
                FromAbstractionId = _abstractionId,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _system.TriggerEvent(msgOut);
        }

        private void HandleNNarInternalRead(ProtoComm.Message msg)
        {
            var bebDeliverMsg = msg.BebDeliver;
            var nnarInternalReadMsg = bebDeliverMsg.Message.NnarInternalRead;

            var plSendMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.NnarInternalValue,
                NnarInternalValue = new ProtoComm.NnarInternalValue
                {
                    ReadId = nnarInternalReadMsg.ReadId,
                    Timestamp = _nnarEntity.Timestamp,
                    WriterRank = _nnarEntity.WriterRank,
                    Value = _nnarEntity.Value
                },
                SystemId = msg.SystemId,
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
                SystemId = msg.SystemId,
                ToAbstractionId = AbstractionIdUtil.GetChildAbstractionId(_abstractionId, PerfectLink.Name),
                FromAbstractionId = _abstractionId,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _system.TriggerEvent(msgOut);
        }

        private void HandleNNarRead(string systemId)
        {
            _readId++;
            _acks = 0;
            _readList.Clear();
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

            _system.TriggerEvent(msgOut);
        }

        private void HandleNNarWrite(ProtoComm.Message msg)
        {
            var nnarWriteMsg = msg.NnarWrite;

            _readId++;
            _writeVal = new ProtoComm.Value { Defined = true, V = nnarWriteMsg.Value.V };
            _acks = 0;
            _readList.Clear();

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

            _system.TriggerEvent(msgOut);
        }
    }
}
