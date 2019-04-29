using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    public interface ISampledSpanStoreLatencyFilter
    {
        string SpanName { get; }
        long LatencyLowerNs { get; }
        long LatencyUpperNs { get; }
        int MaxSpansToReturn { get; }
    }
}
