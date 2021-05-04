using NewDalgs.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NewDalgs.Abstractions
{
    class EventualLeaderDetector : Abstraction
    {
        public static readonly string Name = "eld";

        private HashSet<ProtoComm.ProcessId> _suspected = new HashSet<ProtoComm.ProcessId>();
        private ProtoComm.ProcessId _leader;

        public EventualLeaderDetector(string abstractionId, System.System system)
            : base(abstractionId, system)
        {
            _system.RegisterAbstraction(new EventuallyPerfectFailureDetector(AbstractionIdUtil.GetChildAbstractionId(_abstractionId, EventuallyPerfectFailureDetector.Name), _system));
        }

        public override bool Handle(ProtoComm.Message msg)
        {
            if (msg.Type == ProtoComm.Message.Types.Type.EpfdSuspect)
            {
                HandleEpfdSuspect(msg);
                return true;
            }

            if (msg.Type == ProtoComm.Message.Types.Type.EpfdRestore)
            {
                HandleEpfdRestore(msg);
                return true;
            }

            return false;
        }

        private void HandleEpfdRestore(ProtoComm.Message msg)
        {
            var procId = msg.EpfdRestore.Process;

            _suspected.Remove(procId);

            HandleInternalCheck();
        }

        private void HandleEpfdSuspect(ProtoComm.Message msg)
        {
            var procId = msg.EpfdSuspect.Process;

            _suspected.Add(procId);

            HandleInternalCheck();
        }

        private void HandleInternalCheck()
        {
            var tmpLeader = ProcessIdUtil.FindMaxRank(_system.Processes.Except(_suspected));
            if (tmpLeader == null)
                return;

            if (!tmpLeader.Equals(_leader))
            {
                _leader = tmpLeader;

                var msg = new ProtoComm.Message
                {
                    Type = ProtoComm.Message.Types.Type.EldTrust,
                    EldTrust = new ProtoComm.EldTrust
                    {
                        Process = _leader
                    },
                    SystemId = _system.SystemId,
                    ToAbstractionId = AbstractionIdUtil.GetParentAbstractionId(_abstractionId),    // TODO check this
                    FromAbstractionId = _abstractionId,
                    MessageUuid = Guid.NewGuid().ToString()
                };

                _system.TriggerEvent(msg);
            }
        }
    }
}
