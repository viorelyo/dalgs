using NewDalgs.Utils;
using System;

namespace NewDalgs.Abstractions
{
    class PerfectLink : Abstraction
    {
        public static readonly string Name = "pl";

        public PerfectLink(string abstractionId, System.System system)
            : base(abstractionId, system)
        {
        }

        public override bool Handle(ProtoComm.Message msg)
        {
            if (msg.Type == ProtoComm.Message.Types.Type.NetworkMessage)
            {
                HandlePlDeliver(msg);
                return true;
            }

            if (msg.Type == ProtoComm.Message.Types.Type.PlSend)
            {
                HandlePlSend(msg);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Wrapping received message into Message(PlDeliver)
        /// </summary>
        private void HandlePlDeliver(ProtoComm.Message msg)
        {
            var networkMsg = msg.NetworkMessage;

            var plDeliverMsg = new ProtoComm.PlDeliver
            {
                Message = networkMsg.Message
            };

            ProtoComm.ProcessId foundProcessId = _system.FindProcessByHostAndPort(networkMsg.SenderHost, networkMsg.SenderListeningPort);
            if (foundProcessId != null)
            {
                plDeliverMsg.Sender = foundProcessId;
            }

            var outMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.PlDeliver,
                PlDeliver = plDeliverMsg,
                SystemId = msg.SystemId,
                FromAbstractionId = _abstractionId,
                ToAbstractionId = AbstractionIdUtil.GetParentAbstractionId(msg.ToAbstractionId),
                MessageUuid = Guid.NewGuid().ToString()
            };

            _system.AddToMessageQueue(outMsg);
        }

        private void HandlePlSend(ProtoComm.Message msg)
        {
            var plSendMsg = msg.PlSend;

            var networkMsg = new ProtoComm.NetworkMessage
            {
                Message = plSendMsg.Message,
                SenderHost = _system.ProcessId.Host,
                SenderListeningPort = _system.ProcessId.Port
            };

            var outMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.NetworkMessage,
                NetworkMessage = networkMsg,
                SystemId = plSendMsg.Message.SystemId,
                FromAbstractionId = _abstractionId,
                ToAbstractionId = msg.ToAbstractionId,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _system.SendMessageOverNetwork(outMsg, plSendMsg.Destination.Host, plSendMsg.Destination.Port);
        }
    }
}
