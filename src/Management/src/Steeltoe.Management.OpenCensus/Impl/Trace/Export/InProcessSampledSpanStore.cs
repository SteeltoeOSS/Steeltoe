using Steeltoe.Management.Census.Internal;
using Steeltoe.Management.Census.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class InProcessSampledSpanStore : SampledSpanStoreBase
    {
        private const int NUM_SAMPLES_PER_LATENCY_BUCKET = 10;
        private const int NUM_SAMPLES_PER_ERROR_BUCKET = 5;
        private const long TIME_BETWEEN_SAMPLES = 1000000000;  //TimeUnit.SECONDS.toNanos(1);
        private static readonly int NUM_LATENCY_BUCKETS = LatencyBucketBoundaries.Values.Count;
        // The total number of canonical codes - 1 (the OK code).
        private const int NUM_ERROR_BUCKETS = 17 - 1; // CanonicalCode.values().length - 1;
        private readonly int MAX_PER_SPAN_NAME_SAMPLES =
            NUM_SAMPLES_PER_LATENCY_BUCKET * NUM_LATENCY_BUCKETS
                + NUM_SAMPLES_PER_ERROR_BUCKET * NUM_ERROR_BUCKETS;

        private readonly IEventQueue eventQueue;
        private readonly Dictionary<string, PerSpanNameSamples> samples;

        internal InProcessSampledSpanStore(IEventQueue eventQueue)
        {
            samples = new Dictionary<string, PerSpanNameSamples>();
            this.eventQueue = eventQueue;
        }
        public override ISampledSpanStoreSummary Summary
        {
            get
            {
                Dictionary<string, ISampledPerSpanNameSummary> ret = new Dictionary<string, ISampledPerSpanNameSummary>();
                lock (samples)
                {
                    foreach (var it in samples)
                    {
                        ret[it.Key] = SampledPerSpanNameSummary.Create(it.Value.GetNumbersOfLatencySampledSpans(), it.Value.GetNumbersOfErrorSampledSpans());
                    }
                }

                return SampledSpanStoreSummary.Create(ret);
            }
        }

        public override ISet<string> RegisteredSpanNamesForCollection
        {
            get
            {
                lock (samples)
                {
                    return new HashSet<string>(samples.Keys);
                }
            }
        }

        public override void ConsiderForSampling(ISpan ispan)
        {
            SpanBase span = ispan as SpanBase;
            if (span != null)
            {
                lock (samples)
                {
                    string spanName = span.Name;
                    if (span.IsSampleToLocalSpanStore && !samples.ContainsKey(spanName))
                    {
                        samples[spanName] = new PerSpanNameSamples();
                    }
                    samples.TryGetValue(spanName, out PerSpanNameSamples perSpanNameSamples);
                    if (perSpanNameSamples != null)
                    {
                        perSpanNameSamples.ConsiderForSampling(span);
                    }
                }
            }
        }

        public override IList<ISpanData> GetErrorSampledSpans(ISampledSpanStoreErrorFilter filter)
        {
            int numSpansToReturn = filter.MaxSpansToReturn == 0 ? MAX_PER_SPAN_NAME_SAMPLES : filter.MaxSpansToReturn;
            IList<SpanBase> spans = new List<SpanBase>();
            // Try to not keep the lock to much, do the SpanImpl -> SpanData conversion outside the lock.
            lock(samples) {
                PerSpanNameSamples perSpanNameSamples = samples[filter.SpanName];
                if (perSpanNameSamples != null)
                {
                    spans = perSpanNameSamples.GetErrorSamples(filter.CanonicalCode, numSpansToReturn);
                }
            }
            List<ISpanData> ret = new List<ISpanData>(spans.Count);
            foreach (SpanBase span in spans)
            {
                ret.Add(span.ToSpanData());
            }
            return ret.AsReadOnly();
        }

        public override IList<ISpanData> GetLatencySampledSpans(ISampledSpanStoreLatencyFilter filter)
        {
            int numSpansToReturn = filter.MaxSpansToReturn == 0 ? MAX_PER_SPAN_NAME_SAMPLES : filter.MaxSpansToReturn;
            IList<SpanBase> spans = new List<SpanBase>();
            // Try to not keep the lock to much, do the SpanImpl -> SpanData conversion outside the lock.
            lock(samples) {
                PerSpanNameSamples perSpanNameSamples = samples[filter.SpanName];
                if (perSpanNameSamples != null)
                {
                    spans = perSpanNameSamples.GetLatencySamples(filter.LatencyLowerNs, filter.LatencyUpperNs, numSpansToReturn);
                }
            }
            List<ISpanData> ret = new List<ISpanData>(spans.Count);
            foreach (SpanBase span in spans)
            {
                ret.Add(span.ToSpanData());
            }
            return ret.AsReadOnly();
        }

        public override void RegisterSpanNamesForCollection(IList<string> spanNames)
        {
            eventQueue.Enqueue(new RegisterSpanNameEvent(this, spanNames));
        }

        public override void UnregisterSpanNamesForCollection(IList<string> spanNames)
        {
            eventQueue.Enqueue(new UnregisterSpanNameEvent(this, spanNames));
        }

        internal void InternalUnregisterSpanNamesForCollection(ICollection<string> spanNames)
        {
            lock (samples)
            {
                foreach(string spanName in spanNames)
                {
                    samples.Remove(spanName);
                }
            }
        }
        internal void InternaltRegisterSpanNamesForCollection(ICollection<string> spanNames)
        {
            lock (samples)
            {
                foreach (string spanName in spanNames)
                {
                    if (!samples.ContainsKey(spanName))
                    {
                        samples[spanName] = new PerSpanNameSamples();
                    }
                }
            }
        }

        private sealed class Bucket
        {

            private readonly EvictingQueue<SpanBase> sampledSpansQueue;
            private readonly EvictingQueue<SpanBase> notSampledSpansQueue;
            private long lastSampledNanoTime;
            private long lastNotSampledNanoTime;

            public Bucket(int numSamples)
            {
                sampledSpansQueue = new EvictingQueue<SpanBase>(numSamples);
                notSampledSpansQueue = new EvictingQueue<SpanBase>(numSamples);
            }

            public void ConsiderForSampling(SpanBase span)
            {
                long spanEndNanoTime = span.EndNanoTime;
                if (span.Context.TraceOptions.IsSampled)
                {
                    // Need to compare by doing the subtraction all the time because in case of an overflow,
                    // this may never sample again (at least for the next ~200 years). No real chance to
                    // overflow two times because that means the process runs for ~200 years.
                    if (spanEndNanoTime - lastSampledNanoTime > TIME_BETWEEN_SAMPLES)
                    {
                        sampledSpansQueue.Add(span);
                        lastSampledNanoTime = spanEndNanoTime;
                    }
                }
                else
                {
                    // Need to compare by doing the subtraction all the time because in case of an overflow,
                    // this may never sample again (at least for the next ~200 years). No real chance to
                    // overflow two times because that means the process runs for ~200 years.
                    if (spanEndNanoTime - lastNotSampledNanoTime > TIME_BETWEEN_SAMPLES)
                    {
                        notSampledSpansQueue.Add(span);
                        lastNotSampledNanoTime = spanEndNanoTime;
                    }
                }
            }

            public void GetSamples(int maxSpansToReturn, List<SpanBase> output)
            {
                GetSamples(maxSpansToReturn, output, sampledSpansQueue);
                GetSamples(maxSpansToReturn, output, notSampledSpansQueue);
            }

            public static void GetSamples(
                int maxSpansToReturn, List<SpanBase> output, EvictingQueue<SpanBase> queue)
            {
                SpanBase[] copy = queue.ToArray();

                foreach (SpanBase span in copy)
                {
                    if (output.Count >= maxSpansToReturn)
                    {
                        break;
                    }
                    output.Add(span);
                }
            }

            public void GetSamplesFilteredByLatency(
                long latencyLowerNs, long latencyUpperNs, int maxSpansToReturn, List<SpanBase> output)
            {
                GetSamplesFilteredByLatency(
                    latencyLowerNs, latencyUpperNs, maxSpansToReturn, output, sampledSpansQueue);
                GetSamplesFilteredByLatency(
                    latencyLowerNs, latencyUpperNs, maxSpansToReturn, output, notSampledSpansQueue);
            }

            public static void GetSamplesFilteredByLatency(
                long latencyLowerNs,
                long latencyUpperNs,
                int maxSpansToReturn,
                List<SpanBase> output,
                EvictingQueue<SpanBase> queue)
            {
                SpanBase[] copy = queue.ToArray();
                foreach (SpanBase span in copy)
                {
                    if (output.Count >= maxSpansToReturn)
                    {
                        break;
                    }
                    long spanLatencyNs = span.LatencyNs;
                    if (spanLatencyNs >= latencyLowerNs && spanLatencyNs < latencyUpperNs)
                    {
                        output.Add(span);
                    }
                }
            }

            public int GetNumSamples()
            {
                return sampledSpansQueue.Count + notSampledSpansQueue.Count;
            }
        }
        private sealed class PerSpanNameSamples
        {

            private readonly Bucket[] latencyBuckets;
            private readonly Bucket[] errorBuckets;

            public PerSpanNameSamples()
            {
                latencyBuckets = new Bucket[NUM_LATENCY_BUCKETS];
                for (int i = 0; i < NUM_LATENCY_BUCKETS; i++)
                {
                    latencyBuckets[i] = new Bucket(NUM_SAMPLES_PER_LATENCY_BUCKET);
                }
                errorBuckets = new Bucket[NUM_ERROR_BUCKETS];
                for (int i = 0; i < NUM_ERROR_BUCKETS; i++)
                {
                    errorBuckets[i] = new Bucket(NUM_SAMPLES_PER_ERROR_BUCKET);
                }
            }


            public Bucket GetLatencyBucket(long latencyNs)
            {
                for (int i = 0; i < NUM_LATENCY_BUCKETS; i++)
                {
                    ISampledLatencyBucketBoundaries boundaries = LatencyBucketBoundaries.Values[i];
                    if (latencyNs >= boundaries.LatencyLowerNs
                        && latencyNs < boundaries.LatencyUpperNs)
                    {
                        return latencyBuckets[i];
                    }
                }
                // latencyNs is negative or Long.MAX_VALUE, so this Span can be ignored. This cannot happen
                // in real production because System#nanoTime is monotonic.
                return null;
            }

            public Bucket GetErrorBucket(CanonicalCode code)
            {
                return errorBuckets[(int)code - 1];
            }

            public void ConsiderForSampling(SpanBase span)
            {
                Status status = span.Status;
                // Null status means running Span, this should not happen in production, but the library
                // should not crash because of this.
                if (status != null)
                {
                    Bucket bucket =
                        status.IsOk
                            ? GetLatencyBucket(span.LatencyNs)
                            : GetErrorBucket(status.CanonicalCode);
                    // If unable to find the bucket, ignore this Span.
                    if (bucket != null)
                    {
                        bucket.ConsiderForSampling(span);
                    }
                }
            }

            public IDictionary<ISampledLatencyBucketBoundaries, int> GetNumbersOfLatencySampledSpans()
            {
                IDictionary<ISampledLatencyBucketBoundaries, int> latencyBucketSummaries = new Dictionary<ISampledLatencyBucketBoundaries, int>();
                for (int i = 0; i < NUM_LATENCY_BUCKETS; i++)
                {
                    latencyBucketSummaries[LatencyBucketBoundaries.Values[i]] = latencyBuckets[i].GetNumSamples();
                }
                return latencyBucketSummaries;
            }

            public IDictionary<CanonicalCode, int> GetNumbersOfErrorSampledSpans()
            {
                IDictionary<CanonicalCode, int> errorBucketSummaries = new Dictionary<CanonicalCode, int>();
                for (int i = 0; i < NUM_ERROR_BUCKETS; i++)
                {
                    errorBucketSummaries[(CanonicalCode)i + 1] = errorBuckets[i].GetNumSamples();
                }
                return errorBucketSummaries;
            }

            public IList<SpanBase> GetErrorSamples(CanonicalCode? code, int maxSpansToReturn)
            {
                List<SpanBase> output = new List<SpanBase>(maxSpansToReturn);
                if (code.HasValue)
                {
                    GetErrorBucket(code.Value).GetSamples(maxSpansToReturn, output);
                }
                else
                {
                    for (int i = 0; i < NUM_ERROR_BUCKETS; i++)
                    {
                        errorBuckets[i].GetSamples(maxSpansToReturn, output);
                    }
                }
                return output;
            }

            public IList<SpanBase> GetLatencySamples(long latencyLowerNs, long latencyUpperNs, int maxSpansToReturn)
            {
                List<SpanBase> output = new List<SpanBase>(maxSpansToReturn);
                for (int i = 0; i < NUM_LATENCY_BUCKETS; i++)
                {
                    ISampledLatencyBucketBoundaries boundaries = LatencyBucketBoundaries.Values[i];
                    if (latencyUpperNs >= boundaries.LatencyLowerNs
                        && latencyLowerNs < boundaries.LatencyUpperNs)
                    {
                        latencyBuckets[i].GetSamplesFilteredByLatency(latencyLowerNs, latencyUpperNs, maxSpansToReturn, output);
                    }
                }
                return output;
            }
        }
        private sealed class RegisterSpanNameEvent : IEventQueueEntry
        {
            private readonly InProcessSampledSpanStore sampledSpanStore;
            private readonly ICollection<string> spanNames;

            public RegisterSpanNameEvent(InProcessSampledSpanStore sampledSpanStore, ICollection<string> spanNames)
            {
                this.sampledSpanStore = sampledSpanStore;
                this.spanNames = new List<string>(spanNames);
            }


            public void Process()
            {
                sampledSpanStore.InternaltRegisterSpanNamesForCollection(spanNames);
            }
        }
        private sealed class UnregisterSpanNameEvent : IEventQueueEntry
        {
            private readonly InProcessSampledSpanStore sampledSpanStore;
            private readonly ICollection<string> spanNames;

            public UnregisterSpanNameEvent(InProcessSampledSpanStore sampledSpanStore, ICollection<string> spanNames)
            {
                this.sampledSpanStore = sampledSpanStore;
                this.spanNames = new List<string>(spanNames);
            }

            public void Process()
            {
                sampledSpanStore.InternalUnregisterSpanNamesForCollection(spanNames);
            }
        }
    }
}
