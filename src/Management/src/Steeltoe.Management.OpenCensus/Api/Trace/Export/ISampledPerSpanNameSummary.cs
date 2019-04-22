using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    public interface ISampledPerSpanNameSummary
    {
        IDictionary<ISampledLatencyBucketBoundaries, int> NumbersOfLatencySampledSpans { get; }
        IDictionary<CanonicalCode, int> NumbersOfErrorSampledSpans { get; }
    }
}
