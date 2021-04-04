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
                var networkMsg = msg.NetworkMessage;
                HandlePlDeliver(networkMsg, AbstractionIdUtil.GetParentAbstractionId(msg.ToAbstractionId));
                
                return true;
            }

            if (msg.Type == ProtoComm.Message.Types.Type.PlSend)
            {
                var plSendMsg = msg.PlSend;
                HandlePlSend(plSendMsg.Message, plSendMsg.Destination, msg.ToAbstractionId);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Wrapping received message into Message(PlDeliver)
        /// </summary>
        private void HandlePlDeliver(ProtoComm.NetworkMessage msg, string toAbstractionId)
        {
            var plDeliverMsg = new ProtoComm.PlDeliver
            {
                Message = msg.Message
            };

            ProtoComm.ProcessId foundProcessId = _system.FindProcessByHostAndPort(msg.SenderHost, msg.SenderListeningPort);
            if (foundProcessId != null)
            {
                plDeliverMsg.Sender = foundProcessId;
            }

            var outMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.PlDeliver,
                PlDeliver = plDeliverMsg,
                SystemId = msg.Message.SystemId,
                FromAbstractionId = _abstractionId,
                ToAbstractionId = toAbstractionId,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _system.AddToMessageQueue(outMsg);
        }

        private void HandlePlSend(ProtoComm.Message msg, ProtoComm.ProcessId receiverProcessId, string toAbstractionId)
        {
            var networkMsg = new ProtoComm.NetworkMessage
            {
                Message = msg,
                SenderHost = _system.ProcessId.Host,
                SenderListeningPort = _system.ProcessId.Port
            };

            var outMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.NetworkMessage,
                NetworkMessage = networkMsg,
                SystemId = msg.SystemId,
                FromAbstractionId = _abstractionId,
                ToAbstractionId = toAbstractionId,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _system.SendMessageOverNetwork(outMsg, receiverProcessId.Host, receiverProcessId.Port);
        }
    }
}
