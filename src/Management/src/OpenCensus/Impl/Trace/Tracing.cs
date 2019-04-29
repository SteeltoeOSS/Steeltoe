using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Internal;
using Steeltoe.Management.Census.Trace.Config;
using Steeltoe.Management.Census.Trace.Export;
using Steeltoe.Management.Census.Trace.Internal;
using Steeltoe.Management.Census.Trace.Propagation;
using Steeltoe.Management.Census.Utils;
using System;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class Tracing
    {

        private static Tracing _tracing = new Tracing();

        internal Tracing()
            : this(false)
        {

        }
        internal Tracing(bool enabled)
        {
            if (enabled)
            {
                traceComponent = new TraceComponent(DateTimeOffsetClock.INSTANCE, new RandomGenerator(), new SimpleEventQueue());
            } else
            {
                traceComponent = TraceComponent.NewNoopTraceComponent;
            }
        }

        private ITraceComponent traceComponent = null;


        public static ITracer Tracer
        {
            get
            {
                return _tracing.traceComponent.Tracer;
            }
        }

        public static IPropagationComponent PropagationComponent
        {
            get
            {
                return _tracing.traceComponent.PropagationComponent;
            }
        }

        public static IExportComponent ExportComponent
        {
            get
            {
                return _tracing.traceComponent.ExportComponent;
            }
        }

        public static ITraceConfig TraceConfig
        {
            get
            {
                return _tracing.traceComponent.TraceConfig;
            }
        }


    }
}
