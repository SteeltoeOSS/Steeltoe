using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    public interface IRunningSpanStoreSummary
    {
        IDictionary<string, IRunningPerSpanNameSummary> PerSpanNameSummary { get; }
    }
}
