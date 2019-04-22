using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Config
{
    [Obsolete("Use OpenCensus project packages")]
    public class NoopTraceConfig : TraceConfigBase
    {
        public override ITraceParams ActiveTraceParams
        {
            get { return TraceParams.DEFAULT; }
        }

        public override void UpdateActiveTraceParams(ITraceParams traceParams)
        {
        }
    }
}
