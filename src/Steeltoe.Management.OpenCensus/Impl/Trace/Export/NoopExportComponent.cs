using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    internal sealed class NoopExportComponent : IExportComponent
    {
        private readonly ISampledSpanStore noopSampledSpanStore = Export.SampledSpanStoreBase.NewNoopSampledSpanStore;

        public ISpanExporter SpanExporter
        {
            get
            {
                return Export.SpanExporter.NoopSpanExporter;
            }
        }

        public IRunningSpanStore RunningSpanStore
        {
            get
            {
                return Export.RunningSpanStoreBase.NoopRunningSpanStore;
            }
        }

        public ISampledSpanStore SampledSpanStore
        {
            get
            {
                return noopSampledSpanStore;
            }
        }
    }
}
