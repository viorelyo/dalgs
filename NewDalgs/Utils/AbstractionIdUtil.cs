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

        public static string GetChildAbstractionId(string originalAbstractionId, string childAbstractionId)
        {
            if ((originalAbstractionId == null) || (originalAbstractionId == ""))
                return "";

            if ((childAbstractionId == null) || (childAbstractionId == ""))
                return originalAbstractionId;

            return originalAbstractionId + '.' + childAbstractionId;
        }
    }
}
