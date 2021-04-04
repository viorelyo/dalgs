using NewDalgs.Utils;
using System;

namespace NewDalgs.Abstractions
{
    class NNAtomicRegister : Abstraction
    {
        public static readonly string Name = "nnar";

        public NNAtomicRegister(string abstractionId, System.System system)
            : base(abstractionId, system)
        {
            _system.RegisterAbstraction(new PerfectLink(AbstractionIdUtil.GetChildAbstractionId(_abstractionId, PerfectLink.Name), _system));
            _system.RegisterAbstraction(new BestEffortBroadcast(AbstractionIdUtil.GetChildAbstractionId(_abstractionId, BestEffortBroadcast.Name), _system));
        }

        public override bool Handle(ProtoComm.Message msg)
        {
            if (msg.Type == ProtoComm.Message.Types.Type.NnarWrite)
            {
                HandleNnarWrite(msg);
                return true;
            }

            return false;
        }

        private void HandleNnarWrite(ProtoComm.Message msg)
        {
            
        }
    }
}
