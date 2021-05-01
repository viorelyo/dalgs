using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewDalgs.Utils
{
    static class ProcessIdUtil
    {
        public static ProtoComm.ProcessId FindMaxRank(IEnumerable<ProtoComm.ProcessId> processes)
        {
            // TODO check this
            return processes.OrderBy(procId => procId.Rank).Last();
        }
    }
}
