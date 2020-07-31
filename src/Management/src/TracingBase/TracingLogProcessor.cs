// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenCensus.Trace;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Census.Trace;
using System.Text;

namespace Steeltoe.Management.Tracing
{
    public class TracingLogProcessor : IDynamicMessageProcessor
    {
        private readonly ITracing _tracing;
        private readonly ITracingOptions _options;

        public TracingLogProcessor(ITracingOptions options, ITracing tracing)
        {
            _options = options;
            _tracing = tracing;
        }

        public string Process(string inputLogMessage)
        {
            var currentSpan = GetCurrentSpan();
            if (currentSpan != null)
            {
                var context = currentSpan.Context;
                if (context != null)
                {
                    var sb = new StringBuilder(" [");
                    sb.Append(_options.Name);
                    sb.Append(",");

                    var traceId = context.TraceId.ToLowerBase16();
                    if (traceId.Length > 16 && _options.UseShortTraceIds)
                    {
                        traceId = traceId.Substring(traceId.Length - 16, 16);
                    }

                    sb.Append(traceId);
                    sb.Append(",");

                    sb.Append(context.SpanId.ToLowerBase16());
                    sb.Append(",");

                    if (currentSpan.ParentSpanId != null)
                    {
                        sb.Append(currentSpan.ParentSpanId.ToLowerBase16());
                    }

                    sb.Append(",");

                    if (currentSpan.Options.HasFlag(SpanOptions.RecordEvents))
                    {
                        sb.Append("true");
                    }
                    else
                    {
                        sb.Append("false");
                    }

                    sb.Append("] ");
                    return sb.ToString() + inputLogMessage;
                }
            }

            return inputLogMessage;
        }

        protected internal Span GetCurrentSpan()
        {
            var span = _tracing.Tracer?.CurrentSpan;
            if (span == null || span.Context == SpanContext.Invalid)
            {
                return null;
            }

            return span as Span;
        }
    }
}
