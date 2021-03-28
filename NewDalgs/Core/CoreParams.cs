using System;
using System.Collections.Generic;
using System.Text;

namespace NewDalgs.Core
{
    class CoreParams
    {
        public string Owner { get; set; }
        public string ProcessesHost { get; set; }
        public List<int> ProcessesPorts { get; set; }

        public string SystemId { get; set; }
        public string HubHost { get; set; }
        public int HubPort { get; set; }
    }
}
