using NewDalgs.Utils;

namespace NewDalgs.Abstractions
{
    class BestEffortBroadcast : Abstraction
    {
        public static readonly string Name = "beb";

        public BestEffortBroadcast(string abstractionId, System.System system)
            : base(abstractionId, system)
        {
            _system.RegisterAbstraction(new PerfectLink(AbstractionIdUtil.GetChildAbstractionId(_abstractionId, PerfectLink.Name), _system));
        }

        public override bool Handle(ProtoComm.Message msg)
        {
            if (msg.Type == ProtoComm.Message.Types.Type.BebBroadcast)
            {
                var bebBroadcastMsg = msg.BebBroadcast;
                HandleBebBroadcast(bebBroadcastMsg);

                return true;
            }

            return false;
        }

        private void HandleBebBroadcast(ProtoComm.BebBroadcast msg)
        {
            foreach (var proc in _system.Processes)
            {
                var plSendMsg = new ProtoComm.PlSend
                {
                    Destination = proc,
                    Message = msg.Message
                };

                var outMsg = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.PlSend,
                    PlSend = plSendMsg,
                    FromAbstractionId = _abstractionId,
                    ToAbstractionId = AbstractionIdUtil.GetChildAbstractionId(_abstractionId, PerfectLink.Name)
                };

                _system.AddToMessageQueue(outMsg);
            }
        }
    }
}
