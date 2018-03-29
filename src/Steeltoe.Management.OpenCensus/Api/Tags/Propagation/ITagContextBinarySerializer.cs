using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Tags.Propagation
{
    public interface ITagContextBinarySerializer
    {
        byte[] ToByteArray(ITagContext tags);
        ITagContext FromByteArray(byte[] bytes);
    }
}
