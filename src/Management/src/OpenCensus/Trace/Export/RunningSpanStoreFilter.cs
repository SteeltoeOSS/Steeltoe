// <copyright file="RunningSpanStoreFilter.cs" company="OpenCensus Authors">
// Copyright 2018, OpenCensus Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace OpenCensus.Trace.Export
{
    using System;

    public sealed class RunningSpanStoreFilter : IRunningSpanStoreFilter
    {
        internal RunningSpanStoreFilter(string spanName, int maxSpansToReturn)
        {
            SpanName = spanName ?? throw new ArgumentNullException("Null spanName");
            MaxSpansToReturn = maxSpansToReturn;
        }

        public string SpanName { get; }

        public int MaxSpansToReturn { get; }

        public static IRunningSpanStoreFilter Create(string spanName, int maxSpansToReturn)
        {
            if (maxSpansToReturn < 0)
            {
                throw new ArgumentOutOfRangeException("Negative maxSpansToReturn.");
            }

            return new RunningSpanStoreFilter(spanName, maxSpansToReturn);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "RunningFilter{"
                + "spanName=" + SpanName + ", "
                + "maxSpansToReturn=" + MaxSpansToReturn
                + "}";
        }

    /// <inheritdoc/>
        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (o is RunningSpanStoreFilter that)
            {
                return SpanName.Equals(that.SpanName)
                     && (MaxSpansToReturn == that.MaxSpansToReturn);
            }

            return false;
        }

    /// <inheritdoc/>
        public override int GetHashCode()
        {
            var h = 1;
            h *= 1000003;
            h ^= SpanName.GetHashCode();
            h *= 1000003;
            h ^= MaxSpansToReturn;
            return h;
        }
    }
}
