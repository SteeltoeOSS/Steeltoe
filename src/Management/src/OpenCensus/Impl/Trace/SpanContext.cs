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

namespace Steeltoe.Management.Census.Trace
{
    /// <summary>
    /// A class that represents a span context. A span context contains the state that must propagate to
    /// child <see cref="SpanBase"/> and across process boundaries. It contains the identifiers <see cref="TraceId"/>
    /// and <see cref="SpanId"/> associated with the <see cref="SpanBase"/> and a set of <see cref="TraceOptions"/>.
    /// </summary>
    [Obsolete("Use OpenCensus project packages")]
    public sealed class SpanContext : ISpanContext
    {
        public static readonly SpanContext INVALID = new SpanContext(Trace.TraceId.INVALID, Trace.SpanId.INVALID, TraceOptions.DEFAULT);

        public static ISpanContext Create(ITraceId traceId, ISpanId spanId, TraceOptions traceOptions)
        {
            return new SpanContext(traceId, spanId, traceOptions);
        }

        public ITraceId TraceId { get; }

        public ISpanId SpanId { get; }

        public TraceOptions TraceOptions { get; }

        public bool IsValid => TraceId.IsValid && SpanId.IsValid;

        public override int GetHashCode()
        {
            int result = 1;
            result = (31 * result) + (TraceId == null ? 0 : TraceId.GetHashCode());
            result = (31 * result) + (SpanId == null ? 0 : SpanId.GetHashCode());
            result = (31 * result) + (TraceOptions == null ? 0 : TraceOptions.GetHashCode());
            return result;
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            if (!(obj is SpanContext))
            {
                return false;
            }

            SpanContext that = (SpanContext)obj;
            return TraceId.Equals(that.TraceId)
                && SpanId.Equals(that.SpanId)
                && TraceOptions.Equals(that.TraceOptions);
        }

        public override string ToString()
        {
            return "SpanContext{"
                   + "traceId=" + TraceId + ", "
                   + "spanId=" + SpanId + ", "
                   + "traceOptions=" + TraceOptions
                   + "}";
        }

        private SpanContext(ITraceId traceId, ISpanId spanId, TraceOptions traceOptions)
        {
            TraceId = traceId;
            SpanId = spanId;
            TraceOptions = traceOptions;
        }
    }
}
