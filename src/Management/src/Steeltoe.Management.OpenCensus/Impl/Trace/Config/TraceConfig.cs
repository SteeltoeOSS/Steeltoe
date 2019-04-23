using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Config
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class TraceConfig : TraceConfigBase
    {
        public TraceConfig()
        {
            _activeTraceParams = TraceParams.DEFAULT;
        }

        private ITraceParams _activeTraceParams;
        public override ITraceParams ActiveTraceParams
        {
            get
            {
                return _activeTraceParams;
            }
        }
        public override void UpdateActiveTraceParams(ITraceParams traceParams)
        {
            _activeTraceParams = traceParams;
        }
    }
}
