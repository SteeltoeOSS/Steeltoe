using System;
using System.Collections.Generic;
using System.Text;
using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Trace.Config;
using Steeltoe.Management.Census.Trace.Export;
using Steeltoe.Management.Census.Trace.Propagation;

namespace Steeltoe.Management.Census.Trace
{
    internal sealed class NoopTraceComponent : ITraceComponent
    {
        private readonly IExportComponent noopExportComponent = Export.ExportComponentBase.NewNoopExportComponent;
        public ITracer Tracer
        {
         
            get
            {
                return Trace.Tracer.NoopTracer;
            }
        }

        public IPropagationComponent PropagationComponent
        {
            get
            {
                return Propagation.PropagationComponentBase.NoopPropagationComponent;
            }
        }
        public IClock Clock
        {
            get
            {
                return ZeroTimeClock.INSTANCE;
            }
        }

        public IExportComponent ExportComponent
        {
            get
            {
                return noopExportComponent;
            }
        }
        public ITraceConfig TraceConfig
        {
            get
            {
                return Config.TraceConfigBase.NoopTraceConfig;
            }
        }
    }
}
