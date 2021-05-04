using NewDalgs.Utils;
using System;

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
                HandleBebBroadcast(msg);
                return true;
            }

            if (msg.Type == ProtoComm.Message.Types.Type.PlDeliver)
            {
                HandlePlDeliber(msg);
                return true;
            }

            return false;
        }

        private void HandleBebBroadcast(ProtoComm.Message msg)
        {
            var bebBroadcastMsg = msg.BebBroadcast;

            foreach (var proc in _system.Processes)
            {
                var plSendMsg = new ProtoComm.PlSend
                {
                    Destination = proc,
                    Message = bebBroadcastMsg.Message
                };

                var outMsg = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.PlSend,
                    PlSend = plSendMsg,
                    SystemId = _system.SystemId,
                    FromAbstractionId = _abstractionId,
                    ToAbstractionId = AbstractionIdUtil.GetChildAbstractionId(_abstractionId, PerfectLink.Name),
                    MessageUuid = Guid.NewGuid().ToString()
                };

                _system.TriggerEvent(outMsg);
            }
        }

        private void HandlePlDeliber(ProtoComm.Message msg)
        {
            var plDeliverMsg = msg.PlDeliver;
            var innnerMsg = plDeliverMsg.Message;

            var outMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.BebDeliver,
                BebDeliver = new ProtoComm.BebDeliver
                {
                    Message = innnerMsg,
                    Sender = plDeliverMsg.Sender
                },
                SystemId = _system.SystemId,
                ToAbstractionId = AbstractionIdUtil.GetParentAbstractionId(_abstractionId),
                FromAbstractionId = _abstractionId,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _system.TriggerEvent(outMsg);
        }
    }
}
