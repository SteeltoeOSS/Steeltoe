using System;

namespace Steeltoe.Management.Census.Trace.Config
{
    [Obsolete("Use OpenCensus project packages")]
    public interface ITraceConfig
    {
        ITraceParams ActiveTraceParams { get; }
        void UpdateActiveTraceParams(ITraceParams traceParams);
    }
}