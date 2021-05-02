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

                if (innerMsg.Type == ProtoComm.Message.Types.Type.AppRead)
                {
                    HandleAppRead(innerMsg);
                    return true;
                }

                if (innerMsg.Type == ProtoComm.Message.Types.Type.AppWrite)
                {
                    HandleAppWrite(innerMsg);
                    return true;
                }

                if (innerMsg.Type == ProtoComm.Message.Types.Type.AppPropose)
                {
                    HandleAppPropose(innerMsg);
                    return true;
                }

                return false;
            }

            if (msg.Type == ProtoComm.Message.Types.Type.BebDeliver)
            {
                HandleBebDeliver(msg);
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

            if (msg.Type == ProtoComm.Message.Types.Type.UcDecide)
            {
                HandleUcDecide(msg);
                return true;
            }

            return false;
        }

        private void HandleUcDecide(ProtoComm.Message msg)
        {
            var ucDecideMsg = msg.UcDecide;

            var plSendMsg = new ProtoComm.PlSend
            {
                Destination = _system.HubProcessId,
                Message = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.AppDecide,
                    AppDecide = new ProtoComm.AppDecide
                    {
                        Value = ucDecideMsg.Value
                    },
                    SystemId = msg.SystemId,
                    FromAbstractionId = _abstractionId,
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

            _system.TriggerEvent(outMsg);
        }

        private void HandleAppPropose(ProtoComm.Message msg)
        {
            var appProposeMsg = msg.AppPropose;

            string ucAbstractionId = AbstractionIdUtil.GetUcAbstractionId(_abstractionId, appProposeMsg.Topic);
            _system.RegisterAbstraction(new UniformConsensus(ucAbstractionId, _system));

            var ucProposeMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.UcPropose,
                UcPropose = new ProtoComm.UcPropose
                {
                    Value = appProposeMsg.Value
                },
                SystemId = msg.SystemId,
                FromAbstractionId = _abstractionId,
                ToAbstractionId = ucAbstractionId,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _system.TriggerEvent(ucProposeMsg);
        }

        private void HandleAppRead(ProtoComm.Message msg)
        {
            var appReadMsg = msg.AppRead;

            string nnarAbstractionId = AbstractionIdUtil.GetNnarAbstractionId(_abstractionId, appReadMsg.Register);
            _system.RegisterAbstraction(new NNAtomicRegister(nnarAbstractionId, _system));

            var nnarReadMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.NnarRead,
                NnarRead = new ProtoComm.NnarRead(),
                SystemId = msg.SystemId,
                FromAbstractionId = _abstractionId,
                ToAbstractionId = nnarAbstractionId,
                MessageUuid = Guid.NewGuid().ToString()
            };

            _system.TriggerEvent(nnarReadMsg);
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

            _system.TriggerEvent(outMsg);
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

            _system.TriggerEvent(outMsg);
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

            _system.TriggerEvent(nnarWriteMsg);
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

            _system.TriggerEvent(outMsg);
        }

        private void HandleBebDeliver(ProtoComm.Message msg)
        {
            var bebDeliverMsg = msg.BebDeliver;
            var innerMsg = bebDeliverMsg.Message;

            var plSendMsg = new ProtoComm.PlSend
            {
                Destination = _system.HubProcessId,
                Message = innerMsg
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

            _system.TriggerEvent(outMsg);
        }
    }
}
