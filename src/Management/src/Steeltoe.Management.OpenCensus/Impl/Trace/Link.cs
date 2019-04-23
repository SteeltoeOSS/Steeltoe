using Steeltoe.Management.Census.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class Link : ILink
    {
        private static readonly IDictionary<string, IAttributeValue> EMPTY_ATTRIBUTES = new Dictionary<string, IAttributeValue>();
        public static ILink FromSpanContext(ISpanContext context, LinkType type)
        {
            return new Link(context.TraceId, context.SpanId, type, EMPTY_ATTRIBUTES);
        }
        public static ILink FromSpanContext(ISpanContext context, LinkType type, IDictionary<string, IAttributeValue> attributes)
        {
            IDictionary<string, IAttributeValue> copy = new Dictionary<string, IAttributeValue>(attributes);
            return new Link(
                context.TraceId,
                context.SpanId,
                type,
                new ReadOnlyDictionary<string, IAttributeValue>(copy));
        }
        public ITraceId TraceId { get; }

        public ISpanId SpanId { get; }

        public LinkType Type { get; }

        public IDictionary<string, IAttributeValue> Attributes { get; }

        private Link(ITraceId traceId, ISpanId spanId, LinkType type, IDictionary<string, IAttributeValue> attributes)
        {
            if (traceId == null)
            {
                throw new ArgumentNullException(nameof(traceId));
            }
            if (spanId == null)
            {
                throw new ArgumentNullException(nameof(spanId));
            }
            if (attributes == null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            TraceId = traceId;
            SpanId = spanId;
            Type = type;
            Attributes = attributes;
        }
        public override string ToString()
        {
            return "Link{"
                + "traceId=" + TraceId + ", "
                + "spanId=" + SpanId + ", "
                + "type=" + Type + ", "
                + "attributes=" + Collections.ToString(Attributes)
                + "}";
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is Link)
            {
                Link that = (Link)o;
                return (this.TraceId.Equals(that.TraceId))
                     && (this.SpanId.Equals(that.SpanId))
                     && (this.Type.Equals(that.Type))
                     && (this.Attributes.SequenceEqual(that.Attributes));
            }
            return false;
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= this.TraceId.GetHashCode();
            h *= 1000003;
            h ^= this.SpanId.GetHashCode();
            h *= 1000003;
            h ^= this.Type.GetHashCode();
            h *= 1000003;
            h ^= this.Attributes.GetHashCode();
            return h;
        }
    }
}
