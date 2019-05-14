// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Management.Census.Trace.Sampler;
using System;

namespace Steeltoe.Management.Census.Trace.Config
{
    [Obsolete("Use OpenCensus project packages")]
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

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (o is TraceParams)
            {
                TraceParams that = (TraceParams)o;
                return this.Sampler.Equals(that.Sampler)
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
