using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    public interface ISampledSpanStoreErrorFilter
    {
        string SpanName { get; }
        CanonicalCode? CanonicalCode { get; }
        int MaxSpansToReturn { get; }
    }
}
