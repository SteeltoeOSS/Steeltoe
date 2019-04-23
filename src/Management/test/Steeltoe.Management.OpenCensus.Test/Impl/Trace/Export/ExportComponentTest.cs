using Steeltoe.Management.Census.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Export.Test
{
    public class ExportComponentTest
    {
        private readonly IExportComponent exportComponentWithInProcess = ExportComponent.CreateWithInProcessStores(new SimpleEventQueue());
        private readonly IExportComponent exportComponentWithoutInProcess = ExportComponent.CreateWithoutInProcessStores(new SimpleEventQueue());

        [Fact]
        public void ImplementationOfSpanExporter()
        {
            Assert.IsType<SpanExporter>(exportComponentWithInProcess.SpanExporter);
        }

        [Fact]
        public void ImplementationOfActiveSpans()
        {
            Assert.IsType<InProcessRunningSpanStore>(exportComponentWithInProcess.RunningSpanStore);
            Assert.IsType<NoopRunningSpanStore>(exportComponentWithoutInProcess.RunningSpanStore);
        }

        [Fact]
        public void ImplementationOfSampledSpanStore()
        {
            Assert.IsType<InProcessSampledSpanStore>(exportComponentWithInProcess.SampledSpanStore);
            Assert.IsType<NoopSampledSpanStore>(exportComponentWithoutInProcess.SampledSpanStore);
        }
    }
}
