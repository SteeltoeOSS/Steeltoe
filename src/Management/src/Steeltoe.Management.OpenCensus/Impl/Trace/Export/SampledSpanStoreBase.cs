using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    public abstract class SampledSpanStoreBase : ISampledSpanStore
    {
        private static readonly ISampledSpanStore NOOP_SAMPLED_SPAN_STORE = new NoopSampledSpanStore();

        internal static ISampledSpanStore NoopSampledSpanStore
        {
            get
            {
                return NOOP_SAMPLED_SPAN_STORE;
            }
        }

        internal static ISampledSpanStore NewNoopSampledSpanStore
        {
            get
            {
                return new NoopSampledSpanStore();

            }
        }
        protected SampledSpanStoreBase() { }

        public abstract ISampledSpanStoreSummary Summary { get; }
        public abstract ISet<string> RegisteredSpanNamesForCollection { get; }
        public abstract void ConsiderForSampling(ISpan span);
        public abstract IList<ISpanData> GetErrorSampledSpans(ISampledSpanStoreErrorFilter filter);
        public abstract IList<ISpanData> GetLatencySampledSpans(ISampledSpanStoreLatencyFilter filter);
        public abstract void RegisterSpanNamesForCollection(IList<string> spanNames);
        public abstract void UnregisterSpanNamesForCollection(IList<string> spanNames);
    }
}
