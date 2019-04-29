using System;
using System.Collections.Generic;
using System.Text;

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
            result = 31 * result + (TraceId == null ? 0 : TraceId.GetHashCode());
            result = 31 * result + (SpanId == null ? 0 : SpanId.GetHashCode());
            result = 31 * result + (TraceOptions == null ? 0 : TraceOptions.GetHashCode());
            return result;
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            if (!(obj is SpanContext)) {
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
