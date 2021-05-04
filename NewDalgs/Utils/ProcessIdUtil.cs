using System.Collections.Generic;
using System.Linq;

namespace NewDalgs.Utils
{
    static class ProcessIdUtil
    {
        public static ProtoComm.ProcessId FindMaxRank(IEnumerable<ProtoComm.ProcessId> processes)
        {
            // TODO check this
            return (processes.Count() == 0) ? null : processes.OrderBy(procId => procId.Rank).LastOrDefault();
        }
    }
}
