using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Internal;

namespace Steeltoe.Management.Census.Trace.Export
{
    public sealed class ExportComponent : ExportComponentBase
    {
        private const int EXPORTER_BUFFER_SIZE = 32;
        // Enforces that trace export exports data at least once every 5 seconds.
        private static readonly IDuration EXPORTER_SCHEDULE_DELAY = Duration.Create(5, 0);

        public static IExportComponent CreateWithoutInProcessStores(IEventQueue eventQueue)
        {
            return new ExportComponent(false, eventQueue);
        }
        public static IExportComponent CreateWithInProcessStores(IEventQueue eventQueue)
        {
            return new ExportComponent(true, eventQueue);
        }

        private ExportComponent(bool supportInProcessStores, IEventQueue eventQueue)
        {
            SpanExporter = Export.SpanExporter.Create(EXPORTER_BUFFER_SIZE, EXPORTER_SCHEDULE_DELAY);
            this.RunningSpanStore =
                supportInProcessStores
                    ? new InProcessRunningSpanStore()
                    : Export.RunningSpanStoreBase.NoopRunningSpanStore;
            this.SampledSpanStore =
                supportInProcessStores
                    ? new InProcessSampledSpanStore(eventQueue)
                    : Export.SampledSpanStoreBase.NoopSampledSpanStore;
        }
        public override ISpanExporter SpanExporter { get; }

        public override IRunningSpanStore RunningSpanStore { get; }

        public override ISampledSpanStore SampledSpanStore { get; }
    }
}
