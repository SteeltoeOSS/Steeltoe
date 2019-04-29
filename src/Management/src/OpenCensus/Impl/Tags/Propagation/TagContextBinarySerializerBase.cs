using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Tags.Propagation
{
    [Obsolete("Use OpenCensus project packages")]
    public abstract class TagContextBinarySerializerBase : ITagContextBinarySerializer
    {
        public abstract ITagContext FromByteArray(byte[] bytes);
        public abstract byte[] ToByteArray(ITagContext tags);
    }
}
