using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Tags.Propagation
{
    [Obsolete("Use OpenCensus project packages")]
    public interface ITagContextBinarySerializer
    {
        byte[] ToByteArray(ITagContext tags);
        ITagContext FromByteArray(byte[] bytes);
    }
}
