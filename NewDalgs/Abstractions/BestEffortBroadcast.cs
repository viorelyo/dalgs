﻿using NewDalgs.Utils;

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

            if (msg.Type == ProtoComm.Message.Types.Type.PlDeliver)
            {
                var plDeliverMsg = msg.PlDeliver;
                var innnerMsg = plDeliverMsg.Message;
                if (innnerMsg.Type != ProtoComm.Message.Types.Type.AppValue)
                {
                    return false;
                }

                HandleAppValue(plDeliverMsg.Sender, innnerMsg);

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

        private void HandleAppValue(ProtoComm.ProcessId sender, ProtoComm.Message appValueMsg)
        {
            var outMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.BebDeliver,
                BebDeliver = new ProtoComm.BebDeliver
                {
                    Message = appValueMsg,
                    Sender = sender
                },
                ToAbstractionId = AbstractionIdUtil.GetParentAbstractionId(_abstractionId),
                FromAbstractionId = _abstractionId
            };

            _system.AddToMessageQueue(outMsg);
        }
    }
}
