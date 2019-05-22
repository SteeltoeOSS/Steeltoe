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

using System;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class SampledSpanStoreLatencyFilter : ISampledSpanStoreLatencyFilter
    {
        public static ISampledSpanStoreLatencyFilter Create(string spanName, long latencyLowerNs, long latencyUpperNs, int maxSpansToReturn)
        {
            if (maxSpansToReturn < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSpansToReturn));
            }

            if (latencyLowerNs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(latencyLowerNs));
            }

            if (latencyUpperNs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(latencyUpperNs));
            }

            return new SampledSpanStoreLatencyFilter(spanName, latencyLowerNs, latencyUpperNs, maxSpansToReturn);
        }

        public string SpanName { get; }

        public long LatencyLowerNs { get; }

        public long LatencyUpperNs { get; }

        public int MaxSpansToReturn { get; }

        internal SampledSpanStoreLatencyFilter(string spanName, long latencyLowerNs, long latencyUpperNs, int maxSpansToReturn)
        {
            if (spanName == null)
            {
                throw new ArgumentNullException(nameof(spanName));
            }

            SpanName = spanName;
            LatencyLowerNs = latencyLowerNs;
            LatencyUpperNs = latencyUpperNs;
            MaxSpansToReturn = maxSpansToReturn;
        }

        public override string ToString()
        {
            return "LatencyFilter{"
                + "spanName=" + SpanName + ", "
                + "latencyLowerNs=" + LatencyLowerNs + ", "
                + "latencyUpperNs=" + LatencyUpperNs + ", "
                + "maxSpansToReturn=" + MaxSpansToReturn
                + "}";
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (o is SampledSpanStoreLatencyFilter)
            {
                SampledSpanStoreLatencyFilter that = (SampledSpanStoreLatencyFilter)o;
                return this.SpanName.Equals(that.SpanName)
                     && (this.LatencyLowerNs == that.LatencyLowerNs)
                     && (this.LatencyUpperNs == that.LatencyUpperNs)
                     && (this.MaxSpansToReturn == that.MaxSpansToReturn);
            }

            return false;
        }

        public override int GetHashCode()
        {
            long h = 1;
            h *= 1000003;
            h ^= this.SpanName.GetHashCode();
            h *= 1000003;
            h ^= (this.LatencyLowerNs >> 32) ^ this.LatencyLowerNs;
            h *= 1000003;
            h ^= (this.LatencyUpperNs >> 32) ^ this.LatencyUpperNs;
            h *= 1000003;
            h ^= this.MaxSpansToReturn;
            return (int)h;
        }
    }
}
