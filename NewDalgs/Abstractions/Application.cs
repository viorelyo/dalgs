﻿using NewDalgs.Utils;

namespace NewDalgs.Abstractions
{
    class Application : Abstraction
    {
        public static readonly string Name = "app";

        public Application(string abstractionId, System.System system)
            : base(abstractionId, system)
        {
            _system.RegisterAbstraction(new PerfectLink(AbstractionIdUtil.GetChildAbstractionId(_abstractionId, PerfectLink.Name), _system));
            _system.RegisterAbstraction(new BestEffortBroadcast(AbstractionIdUtil.GetChildAbstractionId(_abstractionId, BestEffortBroadcast.Name), _system));
        }

        public override bool Handle(ProtoComm.Message msg)
        {
            if (msg.Type == ProtoComm.Message.Types.Type.PlDeliver)
            {
                var innerMsg = msg.PlDeliver.Message;
                if (innerMsg.Type == ProtoComm.Message.Types.Type.AppBroadcast)
                {
                    var appBroadcastMsg = innerMsg.AppBroadcast;
                    HandleAppBroadcast(appBroadcastMsg);

                    return true;
                }

                return false;
            }

            if (msg.Type == ProtoComm.Message.Types.Type.BebDeliver)
            {
                var bebDeliverMsg = msg.BebDeliver;
                if (bebDeliverMsg.Message.Type != ProtoComm.Message.Types.Type.AppValue)
                {
                    return false;
                }

                var appValueMsg = bebDeliverMsg.Message;
                HandleAppValue(appValueMsg);

                return true;
            }
            
            return false;
        }

        private void HandleAppBroadcast(ProtoComm.AppBroadcast msg)
        {
            var appValMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.AppValue,
                AppValue = new ProtoComm.AppValue { Value = msg.Value },
                //FromAbstractionId = _abstractionId,   TODO
                //ToAbstractionId = 
            };

            var bebBroadcastMsg = new ProtoComm.BebBroadcast
            {
                Message = appValMsg
            };

            var outMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.BebBroadcast,
                BebBroadcast = bebBroadcastMsg,
                FromAbstractionId = _abstractionId,
                ToAbstractionId = AbstractionIdUtil.GetChildAbstractionId(_abstractionId, BestEffortBroadcast.Name)
            };

            _system.AddToMessageQueue(outMsg);
        }

        private void HandleAppValue(ProtoComm.Message appValueMsg)
        {
            var plSendMsg = new ProtoComm.PlSend
            {
                Destination = _system.HubProcessId,
                Message = appValueMsg
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