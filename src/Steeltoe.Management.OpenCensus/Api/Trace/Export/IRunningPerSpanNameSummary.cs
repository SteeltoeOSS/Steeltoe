using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    public interface IRunningPerSpanNameSummary
    {
        int NumRunningSpans { get; }
    }
}
