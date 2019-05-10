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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (o is SampledPerSpanNameSummary)
            {
                SampledPerSpanNameSummary that = (SampledPerSpanNameSummary)o;
                return this.NumbersOfLatencySampledSpans.SequenceEqual(that.NumbersOfLatencySampledSpans)
                     && this.NumbersOfErrorSampledSpans.SequenceEqual(that.NumbersOfErrorSampledSpans);
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
