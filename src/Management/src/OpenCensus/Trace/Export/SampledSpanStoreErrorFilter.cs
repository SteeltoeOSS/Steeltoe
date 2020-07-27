// <copyright file="SampledSpanStoreErrorFilter.cs" company="OpenCensus Authors">
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

    public sealed class SampledSpanStoreErrorFilter : ISampledSpanStoreErrorFilter
    {
        internal SampledSpanStoreErrorFilter(string spanName, CanonicalCode? canonicalCode, int maxSpansToReturn)
        {
            SpanName = spanName ?? throw new ArgumentNullException(nameof(spanName));
            CanonicalCode = canonicalCode;
            MaxSpansToReturn = maxSpansToReturn;
        }

        public string SpanName { get; }

        public CanonicalCode? CanonicalCode { get; }

        public int MaxSpansToReturn { get; }

        public static ISampledSpanStoreErrorFilter Create(string spanName, CanonicalCode? canonicalCode, int maxSpansToReturn)
        {
            if (canonicalCode == Trace.CanonicalCode.Ok)
            {
                throw new ArgumentOutOfRangeException("Invalid canonical code.");
            }

            if (maxSpansToReturn < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSpansToReturn));
            }

            return new SampledSpanStoreErrorFilter(spanName, canonicalCode, maxSpansToReturn);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "ErrorFilter{"
                + "spanName=" + SpanName + ", "
                + "canonicalCode=" + CanonicalCode + ", "
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

            if (o is SampledSpanStoreErrorFilter that)
            {
                return SpanName.Equals(that.SpanName)
                     && (CanonicalCode == that.CanonicalCode)
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
            h ^= CanonicalCode.GetHashCode();
            h *= 1000003;
            h ^= MaxSpansToReturn;
            return h;
        }
    }
}
