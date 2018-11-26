using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class SampledPerSpanNameSummary : ISampledPerSpanNameSummary
    {
        public IDictionary<ISampledLatencyBucketBoundaries, int> NumbersOfLatencySampledSpans { get; }

        public IDictionary<CanonicalCode, int> NumbersOfErrorSampledSpans { get; }

        public static ISampledPerSpanNameSummary Create(IDictionary<ISampledLatencyBucketBoundaries, int> numbersOfLatencySampledSpans, IDictionary<CanonicalCode, int> numbersOfErrorSampledSpans)
        {
            if (numbersOfLatencySampledSpans == null)
            {
                throw new ArgumentNullException(nameof(numbersOfLatencySampledSpans));
            }
            if (numbersOfErrorSampledSpans == null)
            {
                throw new ArgumentNullException(nameof(numbersOfErrorSampledSpans));
            }

            IDictionary<ISampledLatencyBucketBoundaries, int> copy1 = new Dictionary<ISampledLatencyBucketBoundaries, int>(numbersOfLatencySampledSpans);
            IDictionary<CanonicalCode, int> copy2 = new Dictionary<CanonicalCode, int>(numbersOfErrorSampledSpans);
            return new SampledPerSpanNameSummary(new ReadOnlyDictionary<ISampledLatencyBucketBoundaries, int>(copy1), new ReadOnlyDictionary<CanonicalCode, int>(copy2));
        }

        internal SampledPerSpanNameSummary(IDictionary<ISampledLatencyBucketBoundaries, int> numbersOfLatencySampledSpans, IDictionary<CanonicalCode, int> numbersOfErrorSampledSpans)
        {
            if (numbersOfLatencySampledSpans == null)
            {
                throw new ArgumentNullException(nameof(numbersOfLatencySampledSpans));
            }
            NumbersOfLatencySampledSpans = numbersOfLatencySampledSpans;
            if (numbersOfErrorSampledSpans == null)
            {
                throw new ArgumentNullException(nameof(numbersOfErrorSampledSpans));
            }
            this.NumbersOfErrorSampledSpans = numbersOfErrorSampledSpans;
        }


        public override string ToString()
        {
            return "SampledPerSpanNameSummary{"
                + "numbersOfLatencySampledSpans=" + NumbersOfLatencySampledSpans + ", "
                + "numbersOfErrorSampledSpans=" + NumbersOfErrorSampledSpans
                + "}";
        }


        public override bool Equals(Object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is SampledPerSpanNameSummary)
            {
                SampledPerSpanNameSummary that = (SampledPerSpanNameSummary)o;
                return (this.NumbersOfLatencySampledSpans.SequenceEqual(that.NumbersOfLatencySampledSpans))
                     && (this.NumbersOfErrorSampledSpans.SequenceEqual(that.NumbersOfErrorSampledSpans));
            }
            return false;
        }


        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= this.NumbersOfLatencySampledSpans.GetHashCode();
            h *= 1000003;
            h ^= this.NumbersOfErrorSampledSpans.GetHashCode();
            return h;
        }
    }
}
