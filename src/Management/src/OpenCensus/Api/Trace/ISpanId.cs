using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public interface ISpanId : IComparable<ISpanId>
    {
        byte[] Bytes { get; }
        bool IsValid { get; }
        void CopyBytesTo(byte[] dest, int destOffset);
        string ToLowerBase16();
    }
}
