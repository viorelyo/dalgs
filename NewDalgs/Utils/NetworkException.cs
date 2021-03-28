using System;
using System.Collections.Generic;
using System.Text;

namespace NewDalgs.Utils
{
    class NetworkException : Exception
    {
        public NetworkException()
        {
        }

        public NetworkException(string message)
            : base(message)
        {
        }

        public NetworkException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
