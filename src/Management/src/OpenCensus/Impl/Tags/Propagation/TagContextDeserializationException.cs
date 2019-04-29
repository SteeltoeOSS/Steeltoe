using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Tags.Propagation
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class TagContextDeserializationException : Exception
    {
        public TagContextDeserializationException(String message) 
            : base(message)
        {

        }
        public TagContextDeserializationException(String message, Exception cause)
            : base(message, cause)
        {

        }
    }
}
