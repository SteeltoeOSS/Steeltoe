// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry.Trace;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.OpenTelemetry.Trace;
using System.Text;

namespace Steeltoe.Management.Tracing;

public class TracingLogProcessor : IDynamicMessageProcessor
{
    private readonly ITracingOptions _options;

    public TracingLogProcessor(ITracingOptions options)
    {
        _options = options;
    }

    public string Process(string inputLogMessage)
    {
        var currentSpan = GetCurrentSpan();
        if (currentSpan != null)
        {
            var context = currentSpan.Context;

            var sb = new StringBuilder(" [");
            sb.Append(_options.Name);
            sb.Append(',');

            var traceId = context.TraceId.ToHexString();
            if (traceId.Length > 16 && _options.UseShortTraceIds)
            {
                traceId = traceId.Substring(traceId.Length - 16, 16);
            }

            sb.Append(traceId);
            sb.Append(',');

            sb.Append(context.SpanId.ToHexString());
            sb.Append(',');

            sb.Append(currentSpan.ParentSpanId.ToString());
            sb.Append(',');

            sb.Append(currentSpan.IsRecording ? "true" : "false");

            sb.Append("] ");
            sb.Append(inputLogMessage);

            return sb.ToString();
        }

        return inputLogMessage;
    }

    protected internal TelemetrySpan GetCurrentSpan()
    {
        var span = Tracer.CurrentSpan;
        if (span == null || !span.Context.IsValid)
        {
            return null;
        }

        return span;
    }
}
