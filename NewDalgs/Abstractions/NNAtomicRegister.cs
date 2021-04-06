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
        private int _rId = 0;
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
                HandleNNarRead();
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
            throw new NotImplementedException();
        }

        private void HandleNNarInternalValue(ProtoComm.PlDeliver plDeliverMsg)
        {
            throw new NotImplementedException();
        }

        private void HandleNNarInternalWrite(ProtoComm.BebDeliver bebDeliverMsg)
        {
            throw new NotImplementedException();
        }

        private void HandleNNarInternalRead(ProtoComm.BebDeliver bebDeliverMsg)
        {
            throw new NotImplementedException();
        }

        private void HandleNNarRead()
        {
            throw new NotImplementedException();
        }

        private void HandleNNarWrite(ProtoComm.Message msg)
        {
            
        }
    }
}
