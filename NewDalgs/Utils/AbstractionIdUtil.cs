using NewDalgs.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace NewDalgs.Utils
{
    static class AbstractionIdUtil
    {
        public static string GetParentAbstractionId(string originalAbstractionId)
        {
            if ((originalAbstractionId == null) || (originalAbstractionId == ""))
                return "";

            int pos = originalAbstractionId.LastIndexOf('.');
            if (pos < 0)
                return originalAbstractionId;

            return originalAbstractionId[0..pos];
        }

        public static string GetChildAbstractionId(string parentAbstractionId, string childAbstractionId)
        {
            if ((parentAbstractionId == null) || (parentAbstractionId == ""))
                return "";

            if ((childAbstractionId == null) || (childAbstractionId == ""))
                return parentAbstractionId;

            return parentAbstractionId + '.' + childAbstractionId;
        }

        public static string GetNnarAbstractionId(string parentAbstractionId, string nnarId)
        {
            if ((parentAbstractionId == null) || (parentAbstractionId == ""))
                return "";

            return parentAbstractionId + '.' + NNAtomicRegister.Name + '[' + nnarId + ']';
        }

        public static string GetNnarRegisterName(string nnarAbstractionId)
        {
            int nnarKeywordIndex = nnarAbstractionId.IndexOf(NNAtomicRegister.Name);
            if (nnarKeywordIndex < 0)
                return "";

            var nnarIdSubstring = nnarAbstractionId.Substring(nnarKeywordIndex);

            int openingNnarScopeIndex = NNAtomicRegister.Name.Length;
            if (nnarIdSubstring[openingNnarScopeIndex] != '[')
                return "";

            int closingNnarScopeIndex = nnarIdSubstring.IndexOf(']');
            if (closingNnarScopeIndex < 0)
                return "";

            return nnarIdSubstring.Substring(openingNnarScopeIndex + 1, closingNnarScopeIndex - openingNnarScopeIndex - 1);
        }
    }
}
