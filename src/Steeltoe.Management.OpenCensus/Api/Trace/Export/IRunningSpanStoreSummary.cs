using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    public interface IRunningSpanStoreSummary
    {
        IDictionary<string, IRunningPerSpanNameSummary> PerSpanNameSummary { get; }
    }
}
