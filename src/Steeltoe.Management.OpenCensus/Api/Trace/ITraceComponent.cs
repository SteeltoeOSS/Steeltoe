

using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Trace.Config;
using Steeltoe.Management.Census.Trace.Export;
using Steeltoe.Management.Census.Trace.Propagation;

namespace Steeltoe.Management.Census.Trace
{
    public interface ITraceComponent
    {
        ITracer Tracer { get; }
        IPropagationComponent PropagationComponent { get; }
        IClock Clock { get; }
        IExportComponent ExportComponent { get; }
        ITraceConfig TraceConfig { get; }
    }
}
