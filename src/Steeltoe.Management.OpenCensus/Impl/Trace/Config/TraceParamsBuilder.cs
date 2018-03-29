using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Config
{
    public sealed class TraceParamsBuilder
    {
        private ISampler sampler;
        private int? maxNumberOfAttributes;
        private int? maxNumberOfAnnotations;
        private int? maxNumberOfMessageEvents;
        private int? maxNumberOfLinks;

        internal TraceParamsBuilder(TraceParams source)
        {
            this.sampler = source.Sampler;
            this.maxNumberOfAttributes = source.MaxNumberOfAttributes;
            this.maxNumberOfAnnotations = source.MaxNumberOfAnnotations;
            this.maxNumberOfMessageEvents = source.MaxNumberOfMessageEvents;
            this.maxNumberOfLinks = source.MaxNumberOfLinks;
        }

        public TraceParamsBuilder SetSampler(ISampler sampler)
        {
            if (sampler == null)
            {
                throw new ArgumentNullException("Null sampler");
            }
            this.sampler = sampler;
            return this;
        }
        public TraceParamsBuilder SetMaxNumberOfAttributes(int maxNumberOfAttributes)
        {
            this.maxNumberOfAttributes = maxNumberOfAttributes;
            return this;
        }
        public TraceParamsBuilder SetMaxNumberOfAnnotations(int maxNumberOfAnnotations)
        {
            this.maxNumberOfAnnotations = maxNumberOfAnnotations;
            return this;
        }
        public TraceParamsBuilder SetMaxNumberOfMessageEvents(int maxNumberOfMessageEvents)
        {
            this.maxNumberOfMessageEvents = maxNumberOfMessageEvents;
            return this;
        }
        public TraceParamsBuilder SetMaxNumberOfLinks(int maxNumberOfLinks)
        {
            this.maxNumberOfLinks = maxNumberOfLinks;
            return this;
        }

        public TraceParams Build()
        {
            string missing = string.Empty;
            if (this.sampler == null)
            {
                missing += " sampler";
            }
            if (!this.maxNumberOfAttributes.HasValue)
            {
                missing += " maxNumberOfAttributes";
            }
            if (!this.maxNumberOfAnnotations.HasValue)
            {
                missing += " maxNumberOfAnnotations";
            }
            if (!this.maxNumberOfMessageEvents.HasValue)
            {
                missing += " maxNumberOfMessageEvents";
            }
            if (!this.maxNumberOfLinks.HasValue)
            {
                missing += " maxNumberOfLinks";
            }
            if (!string.IsNullOrEmpty(missing))
            {
                throw new ArgumentOutOfRangeException("Missing required properties:" + missing);
            }

            return new TraceParams(
                this.sampler,
                this.maxNumberOfAttributes.Value,
                this.maxNumberOfAnnotations.Value,
                this.maxNumberOfMessageEvents.Value,
                this.maxNumberOfLinks.Value);
        }
    }
}
