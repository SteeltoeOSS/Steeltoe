using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Trace.Config;
using Steeltoe.Management.Census.Trace.Export;
using Steeltoe.Management.Census.Trace.Propagation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace
{
    public abstract class TraceComponentBase : ITraceComponent
    {
        internal static ITraceComponent NewNoopTraceComponent
        {
            get
            {
                return new NoopTraceComponent();
            }
        }

        public abstract ITracer Tracer { get; }

        public abstract IPropagationComponent PropagationComponent { get; }

        public abstract IClock Clock { get; }

        public abstract IExportComponent ExportComponent { get; }

        public abstract ITraceConfig TraceConfig { get; }
    }
}
