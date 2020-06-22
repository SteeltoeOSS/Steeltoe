﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry.Trace;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.OpenTelemetry.Trace;
using System.Text;

namespace Steeltoe.Management.Tracing
{
    public class TracingLogProcessor : IDynamicMessageProcessor
    {
        private readonly ITracing tracing;
        private readonly ITracingOptions options;

        public TracingLogProcessor(ITracingOptions options, ITracing tracing)
        {
            this.options = options;
            this.tracing = tracing;
        }

        public string Process(string inputLogMessage)
        {
            var currentSpan = GetCurrentSpan();
            if (currentSpan != null)
            {
                var context = currentSpan.Context;

                var sb = new StringBuilder(" [");
                sb.Append(options.Name);
                sb.Append(",");

                var traceId = context.TraceId.ToHexString();
                if (traceId.Length > 16 && options.UseShortTraceIds)
                {
                    traceId = traceId.Substring(traceId.Length - 16, 16);
                }

                sb.Append(traceId);
                sb.Append(",");

                sb.Append(context.SpanId.ToHexString());
                sb.Append(",");

                // TODO: Currently OTel doesnt support getting parentId from Active spans.
                /*
                //if (currentSpan.ParentSpanId != null)
                //{
                //    sb.Append(currentSpan.ParentSpanId.ToLowerBase16());
                //}
                //
                //sb.Append(",");
                */

                if (currentSpan.IsRecording)
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

            return inputLogMessage;
        }

        protected internal TelemetrySpan GetCurrentSpan()
        {
            var span = tracing.Tracer?.CurrentSpan;
            if (span == null || !span.Context.IsValid)
            {
                return null;
            }

            return span;
        }
    }
}
