using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Config
{
    public abstract class TraceConfigBase : ITraceConfig
    {
        private static readonly NoopTraceConfig NOOP_TRACE_CONFIG = new NoopTraceConfig();
        public static ITraceConfig NoopTraceConfig
        {
            get
            {
                return NOOP_TRACE_CONFIG;
            }
        }

        public abstract ITraceParams ActiveTraceParams { get; }
        public abstract void UpdateActiveTraceParams(ITraceParams traceParams);
     
    }
}
