using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    public interface IRunningSpanStoreFilter
    {
        string SpanName { get; }
        int MaxSpansToReturn { get; }
    }
}
