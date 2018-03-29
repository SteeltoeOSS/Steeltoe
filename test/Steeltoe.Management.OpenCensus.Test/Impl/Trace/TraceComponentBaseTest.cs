using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Trace.Config;
using Steeltoe.Management.Census.Trace.Export;
using Steeltoe.Management.Census.Trace.Propagation;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{
    public class TraceComponentBaseTest
    {
        [Fact]
        public void DefaultTracer()
        {
            Assert.Same(Tracer.NoopTracer, TraceComponentBase.NewNoopTraceComponent.Tracer);
        }

        [Fact]
        public void DefaultBinaryPropagationHandler()
        {
            Assert.Same(PropagationComponentBase.NoopPropagationComponent, TraceComponentBase.NewNoopTraceComponent.PropagationComponent);
        }

        [Fact]
        public void DefaultClock()
        {
            Assert.True(TraceComponentBase.NewNoopTraceComponent.Clock is ZeroTimeClock);
        }

        [Fact]
        public void DefaultTraceExporter()
        {
            Assert.Equal(ExportComponentBase.NewNoopExportComponent.GetType(), TraceComponentBase.NewNoopTraceComponent.ExportComponent.GetType());
        }

        [Fact]
        public void DefaultTraceConfig()
        {
            Assert.Same(TraceConfigBase.NoopTraceConfig, TraceComponentBase.NewNoopTraceComponent.TraceConfig);

        }
    }
}
