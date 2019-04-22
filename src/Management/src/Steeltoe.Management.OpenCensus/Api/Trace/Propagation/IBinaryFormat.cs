using Steeltoe.Management.Census.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Propagation
{
    public interface IBinaryFormat
    {
        ISpanContext FromByteArray(byte[] bytes);
        byte[] ToByteArray(ISpanContext spanContext);
    }
}
