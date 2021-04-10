using NewDalgs.Utils;
using System;

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
                    HandleAppBroadcast(innerMsg);
                    return true;
                }

                if (innerMsg.Type == ProtoComm.Message.Types.Type.AppWrite)
                {
                    HandleAppWrite(innerMsg);
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

                HandleAppValue(bebDeliverMsg.Message);
                return true;
            }

            if (msg.Type == ProtoComm.Message.Types.Type.NnarReadReturn)
            {
                HandleNnarReadReturn(msg);
                return true;
            }

            if (msg.Type == ProtoComm.Message.Types.Type.NnarWriteReturn)
            {
                HandleNnarWriteReturn(msg);
                return true;
            }

            return false;
        }

        private void HandleNnarWriteReturn(ProtoComm.Message msg)
        {
            var registerName = AbstractionIdUtil.GetNnarRegisterName(msg.FromAbstractionId);

            var plSendMsg = new ProtoComm.PlSend
            {
                Destination = _system.HubProcessId,
                Message = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.AppWriteReturn,
                    AppWriteReturn = new ProtoComm.AppWriteReturn
                    {
                        Register = registerName
                    },
                    SystemId = msg.SystemId,
                    FromAbstractionId = _abstractionId,
                    //ToAbstractionId = AbstractionIdUtil.GetChildAbstractionId(_abstractionId, PerfectLink.Name),  TODO
                    MessageUuid = Guid.NewGuid().ToString()
                }
            };

            var outMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.PlSend,
                PlSend = plSendMsg,
                SystemId = msg.SystemId,
                FromAbstractionId = _abstractionId,
                ToAbstractionId = AbstractionIdUtil.GetChildAbstractionId(_abstractionId, PerfectLink.Name),
                MessageUuid = Guid.NewGuid().ToString()
            };

            _system.AddToMessageQueue(outMsg);
        }

        private void HandleNnarReadReturn(ProtoComm.Message msg)
        {
            var nnarReadReturnMsg = msg.NnarReadReturn;

            var registerName = AbstractionIdUtil.GetNnarRegisterName(msg.FromAbstractionId);

            var plSendMsg = new ProtoComm.PlSend
            {
                Destination = _system.HubProcessId,
                Message = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.AppReadReturn,
                    AppReadReturn = new ProtoComm.AppReadReturn
                    {
                        Register = registerName,
                        Value = nnarReadReturnMsg.Value
                    },
                    SystemId = msg.SystemId,
                    FromAbstractionId = _abstractionId,
                    //ToAbstractionId = AbstractionIdUtil.GetChildAbstractionId(_abstractionId, PerfectLink.Name),  TODO
                    MessageUuid = Guid.NewGuid().ToString()
                }
            };

            var outMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.PlSend,
                PlSend = plSendMsg,
                SystemId = msg.SystemId,
                FromAbstractionId = _abstractionId,
                ToAbstractionId = AbstractionIdUtil.GetChildAbstractionId(_abstractionId, PerfectLink.Name),
                MessageUuid = Guid.NewGuid().ToString()
            };

            _system.AddToMessageQueue(outMsg);
        }

        private void HandleAppWrite(ProtoComm.Message msg)
        {
            var appWriteMsg = msg.AppWrite;

            string nnarAbstractionId = AbstractionIdUtil.GetNnarAbstractionId(_abstractionId, appWriteMsg.Register);

            _system.RegisterAbstraction(new NNAtomicRegister(nnarAbstractionId, _system));

            var nnarWriteMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.NnarWrite,
                NnarWrite = new ProtoComm.NnarWrite
                {
                   Value = appWriteMsg.Value
                },
                SystemId = msg.SystemId,
                FromAbstractionId = _abstractionId,
                ToAbstractionId = nnarAbstractionId,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _system.AddToMessageQueue(nnarWriteMsg);
        }

        private void HandleAppBroadcast(ProtoComm.Message msg)
        {
            var appBroadcastMsg = msg.AppBroadcast;

            var appValMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.AppValue,
                AppValue = new ProtoComm.AppValue { Value = appBroadcastMsg.Value },
                SystemId = msg.SystemId,
                FromAbstractionId = _abstractionId,
                ToAbstractionId = _abstractionId,
                MessageUuid = Guid.NewGuid().ToString()
            };

            var bebBroadcastMsg = new ProtoComm.BebBroadcast
            {
                Message = appValMsg
            };

            var outMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.BebBroadcast,
                BebBroadcast = bebBroadcastMsg,
                SystemId = msg.SystemId,
                FromAbstractionId = _abstractionId,
                ToAbstractionId = AbstractionIdUtil.GetChildAbstractionId(_abstractionId, BestEffortBroadcast.Name),
                MessageUuid = Guid.NewGuid().ToString()
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
                SystemId = appValueMsg.SystemId,
                FromAbstractionId = _abstractionId,
                ToAbstractionId = AbstractionIdUtil.GetChildAbstractionId(_abstractionId, PerfectLink.Name),
                MessageUuid = Guid.NewGuid().ToString()
            };

            _system.AddToMessageQueue(outMsg);
        }
    }
}
