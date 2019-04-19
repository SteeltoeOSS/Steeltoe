using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    public interface ISampledPerSpanNameSummary
    {
        IDictionary<ISampledLatencyBucketBoundaries, int> NumbersOfLatencySampledSpans { get; }
        IDictionary<CanonicalCode, int> NumbersOfErrorSampledSpans { get; }
    }
}
