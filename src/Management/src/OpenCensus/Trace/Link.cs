// <copyright file="Link.cs" company="OpenCensus Authors">
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

namespace OpenCensus.Trace
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using OpenCensus.Utils;

    public sealed class Link : ILink
    {
        private static readonly IDictionary<string, IAttributeValue> EmptyAttributes = new Dictionary<string, IAttributeValue>();

        private Link(ITraceId traceId, ISpanId spanId, LinkType type, IDictionary<string, IAttributeValue> attributes)
        {
            TraceId = traceId ?? throw new ArgumentNullException(nameof(traceId));
            SpanId = spanId ?? throw new ArgumentNullException(nameof(spanId));
            Type = type;
            Attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
        }

        public ITraceId TraceId { get; }

        public ISpanId SpanId { get; }

        public LinkType Type { get; }

        public IDictionary<string, IAttributeValue> Attributes { get; }

        public static ILink FromSpanContext(ISpanContext context, LinkType type)
        {
            return new Link(context.TraceId, context.SpanId, type, EmptyAttributes);
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

        /// <inheritdoc/>
        public override string ToString()
        {
            return "Link{"
                + "traceId=" + TraceId + ", "
                + "spanId=" + SpanId + ", "
                + "type=" + Type + ", "
                + "attributes=" + Collections.ToString(Attributes)
                + "}";
        }

        /// <inheritdoc/>
        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (o is Link that)
            {
                return TraceId.Equals(that.TraceId)
                     && SpanId.Equals(that.SpanId)
                     && Type.Equals(that.Type)
                     && Attributes.SequenceEqual(that.Attributes);
            }

            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var h = 1;
            h *= 1000003;
            h ^= TraceId.GetHashCode();
            h *= 1000003;
            h ^= SpanId.GetHashCode();
            h *= 1000003;
            h ^= Type.GetHashCode();
            h *= 1000003;
            h ^= Attributes.GetHashCode();
            return h;
        }
    }
}
