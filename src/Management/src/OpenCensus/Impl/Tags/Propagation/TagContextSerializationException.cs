using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Tags.Propagation
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class TagContextSerializationException : Exception
    {
        public TagContextSerializationException(String message)
             : base(message)
        {

        }
        public TagContextSerializationException(String message, Exception cause)
            : base(message, cause)
        {

        }
    }
}
