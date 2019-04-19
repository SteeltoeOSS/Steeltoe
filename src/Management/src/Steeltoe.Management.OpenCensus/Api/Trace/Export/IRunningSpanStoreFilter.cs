using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    public interface IRunningSpanStoreFilter
    {
        string SpanName { get; }
        int MaxSpansToReturn { get; }
    }
}
