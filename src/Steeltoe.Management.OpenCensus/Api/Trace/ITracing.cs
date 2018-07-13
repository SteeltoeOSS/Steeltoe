using Steeltoe.Management.Census.Trace.Config;
using Steeltoe.Management.Census.Trace.Export;
using Steeltoe.Management.Census.Trace.Propagation;

namespace Steeltoe.Management.Census.Trace
{
    public interface ITracing
    {
        ITracer Tracer { get; }
        IPropagationComponent PropagationComponent { get; }
        IExportComponent ExportComponent { get; }
        ITraceConfig TraceConfig { get; }
    }
}
