using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Tags.Propagation
{
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
