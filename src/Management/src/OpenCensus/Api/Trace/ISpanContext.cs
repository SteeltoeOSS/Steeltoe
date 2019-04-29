using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public interface ISpanContext
    {
        ITraceId TraceId { get; }
        ISpanId SpanId { get; }
        TraceOptions TraceOptions { get; }
        bool IsValid { get; }
    }
}
