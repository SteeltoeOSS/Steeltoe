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
    public sealed class RunningSpanStoreFilter : IRunningSpanStoreFilter
    {
        public static IRunningSpanStoreFilter Create(string spanName, int maxSpansToReturn)
        {
            if (maxSpansToReturn < 0)
            {
                throw new ArgumentOutOfRangeException("Negative maxSpansToReturn.");
            }

            return new RunningSpanStoreFilter(spanName, maxSpansToReturn);
        }

        internal RunningSpanStoreFilter(string spanName, int maxSpansToReturn)
        {
            if (spanName == null)
            {
                throw new ArgumentNullException("Null spanName");
            }

            SpanName = spanName;
            MaxSpansToReturn = maxSpansToReturn;
        }

        public string SpanName { get; }

        public int MaxSpansToReturn { get; }

        public override string ToString()
        {
            return "RunningFilter{"
                + "spanName=" + SpanName + ", "
                + "maxSpansToReturn=" + MaxSpansToReturn
                + "}";
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (o is RunningSpanStoreFilter)
            {
                RunningSpanStoreFilter that = (RunningSpanStoreFilter)o;
                return this.SpanName.Equals(that.SpanName)
                     && (this.MaxSpansToReturn == that.MaxSpansToReturn);
            }

            return false;
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= this.SpanName.GetHashCode();
            h *= 1000003;
            h ^= this.MaxSpansToReturn;
            return h;
        }
    }
}
