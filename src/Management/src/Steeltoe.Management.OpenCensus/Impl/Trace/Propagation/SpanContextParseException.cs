using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Propagation
{
    [Obsolete("Use OpenCensus project packages")]
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
