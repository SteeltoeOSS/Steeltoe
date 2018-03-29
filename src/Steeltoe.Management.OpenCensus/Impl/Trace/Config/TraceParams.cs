using Steeltoe.Management.Census.Trace.Sampler;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Config
{
    public sealed class TraceParams : ITraceParams
    {
        private const double DEFAULT_PROBABILITY = 1e-4;
        private static readonly ISampler DEFAULT_SAMPLER = Samplers.GetProbabilitySampler(DEFAULT_PROBABILITY);
        private const int DEFAULT_SPAN_MAX_NUM_ATTRIBUTES = 32;
        private const int DEFAULT_SPAN_MAX_NUM_ANNOTATIONS = 32;
        private const int DEFAULT_SPAN_MAX_NUM_MESSAGE_EVENTS = 128;
        private const int DEFAULT_SPAN_MAX_NUM_LINKS = 128;

        public static readonly ITraceParams DEFAULT = 
            new TraceParams(DEFAULT_SAMPLER, DEFAULT_SPAN_MAX_NUM_ATTRIBUTES, DEFAULT_SPAN_MAX_NUM_ANNOTATIONS, DEFAULT_SPAN_MAX_NUM_MESSAGE_EVENTS, DEFAULT_SPAN_MAX_NUM_LINKS);

        internal TraceParams(ISampler sampler, int maxNumberOfAttributes, int maxNumberOfAnnotations, int maxNumberOfMessageEvents, int maxNumberOfLinks)
        {
            if (sampler == null)
            {
                throw new ArgumentNullException(nameof(sampler));
            }
            if (maxNumberOfAttributes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxNumberOfAttributes));
            }
            if (maxNumberOfAnnotations <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxNumberOfAnnotations));
            }
            if (maxNumberOfMessageEvents <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxNumberOfMessageEvents));
            }
            if (maxNumberOfLinks <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxNumberOfLinks));
            }
            this.Sampler = sampler;
            this.MaxNumberOfAttributes = maxNumberOfAttributes;
            this.MaxNumberOfAnnotations = maxNumberOfAnnotations;
            this.MaxNumberOfMessageEvents = maxNumberOfMessageEvents;
            this.MaxNumberOfLinks = maxNumberOfLinks;
        }

        public ISampler Sampler { get; }
        public int MaxNumberOfAttributes { get; }
        public int MaxNumberOfAnnotations { get; }
        public int MaxNumberOfMessageEvents { get; }
        public int MaxNumberOfLinks { get; }
        public TraceParamsBuilder ToBuilder()
        {
            return new TraceParamsBuilder(this);
        }

        public override string ToString()
        {
            return "TraceParams{"
                + "sampler=" + Sampler + ", "
                + "maxNumberOfAttributes=" + MaxNumberOfAttributes + ", "
                + "maxNumberOfAnnotations=" + MaxNumberOfAnnotations + ", "
                + "maxNumberOfMessageEvents=" + MaxNumberOfMessageEvents + ", "
                + "maxNumberOfLinks=" + MaxNumberOfLinks
                + "}";
        }


        public override Boolean Equals(object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is TraceParams)
            {
                TraceParams that = (TraceParams)o;
                return (this.Sampler.Equals(that.Sampler))
                     && (this.MaxNumberOfAttributes == that.MaxNumberOfAttributes)
                     && (this.MaxNumberOfAnnotations == that.MaxNumberOfAnnotations)
                     && (this.MaxNumberOfMessageEvents == that.MaxNumberOfMessageEvents)
                     && (this.MaxNumberOfLinks == that.MaxNumberOfLinks);
            }
            return false;
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= this.Sampler.GetHashCode();
            h *= 1000003;
            h ^= this.MaxNumberOfAttributes;
            h *= 1000003;
            h ^= this.MaxNumberOfAnnotations;
            h *= 1000003;
            h ^= this.MaxNumberOfMessageEvents;
            h *= 1000003;
            h ^= this.MaxNumberOfLinks;
            return h;
        }
    }
}
