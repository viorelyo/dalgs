using NewDalgs.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewDalgs.Utils
{
    static class EpochConsensusStateUtil
    {
        public static EpochConsensusState FindHighest(IEnumerable<EpochConsensusState> states)
        {
            // TODO check this
            return states.OrderBy(state => state.ValTimestamp).Last();
        }
    }
}
