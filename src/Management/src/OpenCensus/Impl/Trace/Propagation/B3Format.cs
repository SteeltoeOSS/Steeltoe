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

namespace Steeltoe.Management.Census.Trace.Propagation
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class B3Format : TextFormatBase
    {
        public const string X_B3_TRACE_ID = "X-B3-TraceId";
        public const string X_B3_SPAN_ID = "X-B3-SpanId";
        public const string X_B3_PARENT_SPAN_ID = "X-B3-ParentSpanId";
        public const string X_B3_SAMPLED = "X-B3-Sampled";
        public const string X_B3_FLAGS = "X-B3-Flags";
        private static readonly List<string> FIELDS = new List<string>() { X_B3_TRACE_ID, X_B3_SPAN_ID, X_B3_PARENT_SPAN_ID, X_B3_SAMPLED, X_B3_FLAGS };

        // Used as the upper TraceId.SIZE hex characters of the traceID. B3-propagation used to send
        // TraceId.SIZE hex characters (8-bytes traceId) in the past.
        internal const string UPPER_TRACE_ID = "0000000000000000";

        // Sampled value via the X_B3_SAMPLED header.
        internal const string SAMPLED_VALUE = "1";

        // "Debug" sampled value.
        internal const string FLAGS_VALUE = "1";

        public override IList<string> Fields
        {
            get
            {
                return FIELDS.AsReadOnly();
            }
        }

        public override ISpanContext Extract<C>(C carrier, IGetter<C> getter)
        {
            if (carrier == null)
            {
                throw new ArgumentNullException(nameof(carrier));
            }

            if (getter == null)
            {
                throw new ArgumentNullException(nameof(getter));
            }

            try
            {
                ITraceId traceId;
                string traceIdStr = getter.Get(carrier, X_B3_TRACE_ID);
                if (traceIdStr != null)
                {
                    if (traceIdStr.Length == TraceId.SIZE)
                    {
                        // This is an 8-byte traceID.
                        traceIdStr = UPPER_TRACE_ID + traceIdStr;
                    }

                    traceId = TraceId.FromLowerBase16(traceIdStr);
                }
                else
                {
                    throw new SpanContextParseException("Missing X_B3_TRACE_ID.");
                }

                ISpanId spanId;
                string spanIdStr = getter.Get(carrier, X_B3_SPAN_ID);
                if (spanIdStr != null)
                {
                    spanId = SpanId.FromLowerBase16(spanIdStr);
                }
                else
                {
                    throw new SpanContextParseException("Missing X_B3_SPAN_ID.");
                }

                TraceOptions traceOptions = TraceOptions.DEFAULT;
                if (SAMPLED_VALUE.Equals(getter.Get(carrier, X_B3_SAMPLED))
                    || FLAGS_VALUE.Equals(getter.Get(carrier, X_B3_FLAGS)))
                {
                    traceOptions = TraceOptions.Builder().SetIsSampled(true).Build();
                }

                return SpanContext.Create(traceId, spanId, traceOptions);
            }
            catch (Exception e)
            {
                throw new SpanContextParseException("Invalid input.", e);
            }
        }

        public override void Inject<C>(ISpanContext spanContext, C carrier, ISetter<C> setter)
        {
            if (spanContext == null)
            {
                throw new ArgumentNullException(nameof(spanContext));
            }

            if (carrier == null)
            {
                throw new ArgumentNullException(nameof(carrier));
            }

            if (setter == null)
            {
                throw new ArgumentNullException(nameof(setter));
            }

            setter.Put(carrier, X_B3_TRACE_ID, spanContext.TraceId.ToLowerBase16());
            setter.Put(carrier, X_B3_SPAN_ID, spanContext.SpanId.ToLowerBase16());
            if (spanContext.TraceOptions.IsSampled)
            {
                setter.Put(carrier, X_B3_SAMPLED, SAMPLED_VALUE);
            }
        }
    }
}
