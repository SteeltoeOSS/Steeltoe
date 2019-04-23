using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Export.Test
{
    public class NoopRunningSpanStoreTest
    {
        private readonly IRunningSpanStore runningSpanStore = ExportComponent.NewNoopExportComponent.RunningSpanStore;

        [Fact]
        public void NoopRunningSpanStore_GetSummary()
        {
            IRunningSpanStoreSummary summary = runningSpanStore.Summary;
            Assert.Empty(summary.PerSpanNameSummary);
        }

        [Fact]
        public void NoopRunningSpanStore_GetRunningSpans_DisallowsNull()
        {
            Assert.Throws<ArgumentNullException>(() => runningSpanStore.GetRunningSpans(null));
        }

        [Fact]
        public void NoopRunningSpanStore_GetRunningSpans()
        {
            IList<ISpanData> runningSpans = runningSpanStore.GetRunningSpans(RunningSpanStoreFilter.Create("TestSpan", 0));
            Assert.Empty(runningSpans);
        }
    }
}
