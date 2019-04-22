using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Export.Test
{
    public class ExportComponentBaseTest
    {
        private readonly IExportComponent exportComponent = ExportComponentBase.NewNoopExportComponent;

        [Fact]
        public void ImplementationOfSpanExporter()
        {
            Assert.Equal(SpanExporter.NoopSpanExporter, exportComponent.SpanExporter);
        }

        [Fact]
        public void ImplementationOfActiveSpans()
        {
            Assert.Equal(RunningSpanStoreBase.NoopRunningSpanStore, exportComponent.RunningSpanStore);
        }

        [Fact]
        public void ImplementationOfSampledSpanStore()
        {
            Assert.Equal(SampledSpanStoreBase.NewNoopSampledSpanStore.GetType(), exportComponent.SampledSpanStore.GetType());
        }
    }
}
