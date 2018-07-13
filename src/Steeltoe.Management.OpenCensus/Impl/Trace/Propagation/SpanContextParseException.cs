using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Propagation
{
    public class SpanContextParseException : Exception
    {
        public SpanContextParseException(string message)
            : base(message)
        { 
        }
        public SpanContextParseException(string message, Exception cause)
            : base(message, cause)
        { 
        }
    }
}
