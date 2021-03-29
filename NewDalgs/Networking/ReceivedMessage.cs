using System;
using System.Collections.Generic;
using System.Text;

namespace NewDalgs.Networking
{
    class ReceivedMessage
    {
        public ProtoComm.Message Message { get; set; }
        public int SenderListeningPort { get; set; }
        public string ReceivedSystemId { get; set; }
        public string ReceivedToAbstractionId { get; set; }
    }
}
