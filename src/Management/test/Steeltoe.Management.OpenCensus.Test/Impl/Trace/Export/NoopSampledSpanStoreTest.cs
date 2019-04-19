using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Export.Test
{
    public class NoopSampledSpanStoreTest
    {
        //@Rule public final ExpectedException thrown = ExpectedException.none();
        private static readonly ISampledPerSpanNameSummary EMPTY_PER_SPAN_NAME_SUMMARY =
            SampledPerSpanNameSummary.Create(new Dictionary<ISampledLatencyBucketBoundaries, int>(), new Dictionary<CanonicalCode, int>());

        [Fact]
        public void NoopSampledSpanStore_RegisterUnregisterAndGetSummary()
        {
            // should return empty before register
            ISampledSpanStore sampledSpanStore =
                ExportComponent.NewNoopExportComponent.SampledSpanStore;
            ISampledSpanStoreSummary summary = sampledSpanStore.Summary;
            Assert.Empty(summary.PerSpanNameSummary);

            // should return non-empty summaries with zero latency/error sampled spans after register
            sampledSpanStore.RegisterSpanNamesForCollection(
                new List<string>() { "TestSpan1", "TestSpan2", "TestSpan3" });
            summary = sampledSpanStore.Summary;
            Assert.Equal(3, summary.PerSpanNameSummary.Count);
            Assert.Contains(summary.PerSpanNameSummary, (item) =>
            {
                return (item.Key == "TestSpan1" || item.Key == "TestSpan2" || item.Key == "TestSpan3") &&
                item.Value.Equals(EMPTY_PER_SPAN_NAME_SUMMARY); 
            });

            // should unregister specific spanNames
            sampledSpanStore.UnregisterSpanNamesForCollection(new List<string>() { "TestSpan1", "TestSpan3" });
            summary = sampledSpanStore.Summary;
            Assert.Equal(1, summary.PerSpanNameSummary.Count);
            Assert.Contains(summary.PerSpanNameSummary, (item) =>
            {
                return (item.Key == "TestSpan2") && item.Value.Equals(EMPTY_PER_SPAN_NAME_SUMMARY);
            });

        }

        [Fact]
        public void NoopSampledSpanStore_GetLatencySampledSpans()
        {
            ISampledSpanStore sampledSpanStore = ExportComponentBase.NewNoopExportComponent.SampledSpanStore;
            IList<ISpanData> latencySampledSpans =
                sampledSpanStore.GetLatencySampledSpans(
                    SampledSpanStoreLatencyFilter.Create("TestLatencyFilter", 0, 0, 0));
            Assert.Empty(latencySampledSpans);
        }

        [Fact]
        public void NoopSampledSpanStore_GetErrorSampledSpans()
        {
            ISampledSpanStore sampledSpanStore = ExportComponentBase.NewNoopExportComponent.SampledSpanStore;
            IList<ISpanData> errorSampledSpans =
                sampledSpanStore.GetErrorSampledSpans(
                    SampledSpanStoreErrorFilter.Create("TestErrorFilter", null, 0));
            Assert.Empty(errorSampledSpans);
        }

        [Fact]
        public void NoopSampledSpanStore_GetRegisteredSpanNamesForCollection()
        {
            ISampledSpanStore sampledSpanStore = ExportComponentBase.NewNoopExportComponent.SampledSpanStore;
            sampledSpanStore.RegisterSpanNamesForCollection(new List<string>() { "TestSpan3", "TestSpan4" });
            ISet<string> registeredSpanNames = sampledSpanStore.RegisteredSpanNamesForCollection;
            Assert.Equal(2, registeredSpanNames.Count);
            Assert.Contains(registeredSpanNames, (item) =>
            {
                return (item == "TestSpan3" || item == "TestSpan4");
            });
        }
    }
}
