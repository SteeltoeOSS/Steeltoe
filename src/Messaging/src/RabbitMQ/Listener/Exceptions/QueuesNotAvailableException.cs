using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Messaging.Rabbit.Listener.Exceptions
{
    class QueuesNotAvailableException : FatalListenerStartupException
    {
        public QueuesNotAvailableException(string message, Exception cause)
            : base(message, cause)
        {
        }
    }
}
