using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    internal sealed class NoopSampledSpanStore : SampledSpanStoreBase
    {
        private static readonly ISampledPerSpanNameSummary EMPTY_PER_SPAN_NAME_SUMMARY = SampledPerSpanNameSummary.Create(
            new Dictionary<ISampledLatencyBucketBoundaries, int>(), new Dictionary<CanonicalCode, int>());

        private static readonly ISampledSpanStoreSummary EMPTY_SUMMARY = SampledSpanStoreSummary.Create(new Dictionary<string, ISampledPerSpanNameSummary>());
  
        private static readonly IList<ISpanData> EMPTY_SPANDATA = new List<ISpanData>();

        private readonly HashSet<string> registeredSpanNames = new HashSet<string>();
        public override ISampledSpanStoreSummary Summary
        {
            get
            {
                IDictionary<string, ISampledPerSpanNameSummary> result = new Dictionary<string, ISampledPerSpanNameSummary>();
                lock(registeredSpanNames) {
                    foreach (string registeredSpanName in registeredSpanNames)
                    {
                        result[registeredSpanName] = EMPTY_PER_SPAN_NAME_SUMMARY;
                    }
                }
                return SampledSpanStoreSummary.Create(result);
            }
        }

        public override ISet<string> RegisteredSpanNamesForCollection
        {
            get
            {
                return new HashSet<string>(registeredSpanNames);
            }
       
        }

        public override void ConsiderForSampling(ISpan span)
        {
        }

        public override IList<ISpanData> GetErrorSampledSpans(ISampledSpanStoreErrorFilter filter)
        {
            return EMPTY_SPANDATA;
        }

        public override IList<ISpanData> GetLatencySampledSpans(ISampledSpanStoreLatencyFilter filter)
        {
            return EMPTY_SPANDATA;
        }

        public override void RegisterSpanNamesForCollection(IList<string> spanNames)
        {
            if (spanNames == null)
            {
                throw new ArgumentNullException(nameof(spanNames));
            }

            lock(registeredSpanNames)
            {
                foreach (var name in spanNames)
                {
                    registeredSpanNames.Add(name);
                }
            }

        }

        public override void UnregisterSpanNamesForCollection(IList<string> spanNames)
        {
            if (spanNames == null)
            {
                throw new ArgumentNullException(nameof(spanNames));
            }

            lock (registeredSpanNames)
            {
                foreach (var name in spanNames)
                {
                    registeredSpanNames.Remove(name);
                }
            }
        }
    }
}
