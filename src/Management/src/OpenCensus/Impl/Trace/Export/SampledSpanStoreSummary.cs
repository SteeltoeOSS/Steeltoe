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
    public class SampledSpanStoreSummary : ISampledSpanStoreSummary
    {
        public static ISampledSpanStoreSummary Create(IDictionary<string, ISampledPerSpanNameSummary> perSpanNameSummary)
        {
            if (perSpanNameSummary == null)
            {
                throw new ArgumentNullException(nameof(perSpanNameSummary));
            }

            IDictionary<string, ISampledPerSpanNameSummary> copy = new Dictionary<string, ISampledPerSpanNameSummary>(perSpanNameSummary);
            return new SampledSpanStoreSummary(new ReadOnlyDictionary<string, ISampledPerSpanNameSummary>(copy));
        }

        internal SampledSpanStoreSummary(IDictionary<string, ISampledPerSpanNameSummary> perSpanNameSummary)
        {
            if (perSpanNameSummary == null)
            {
                throw new ArgumentNullException(nameof(perSpanNameSummary));
            }

            PerSpanNameSummary = perSpanNameSummary;
        }

        public IDictionary<string, ISampledPerSpanNameSummary> PerSpanNameSummary { get; }

        public override string ToString()
        {
            return "SampledSummary{"
                + "perSpanNameSummary=" + PerSpanNameSummary
                + "}";
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (o is SampledSpanStoreSummary)
            {
                SampledSpanStoreSummary that = (SampledSpanStoreSummary)o;
                return this.PerSpanNameSummary.SequenceEqual(that.PerSpanNameSummary);
            }

            return false;
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= this.PerSpanNameSummary.GetHashCode();
            return h;
        }
    }
}
