using NewDalgs.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace NewDalgs.Utils
{
    static class EpochConsensusStateUtil
    {
        public static EpochConsensusState FindHighest(IEnumerable<EpochConsensusState> states)
        {
            // TODO check this
            return (states.Count() == 0) ? null : states.OrderBy(state => state.ValTimestamp).Last();
        }
    }
}
