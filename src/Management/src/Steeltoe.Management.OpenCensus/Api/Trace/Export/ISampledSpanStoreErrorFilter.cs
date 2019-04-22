using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    public interface ISampledSpanStoreErrorFilter
    {
        string SpanName { get; }
        CanonicalCode? CanonicalCode { get; }
        int MaxSpansToReturn { get; }
    }
}
