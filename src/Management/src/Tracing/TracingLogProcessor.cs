// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using Steeltoe.Logging;

namespace Steeltoe.Management.Tracing;

public sealed class TracingLogProcessor : IDynamicMessageProcessor
{
    private readonly IOptionsMonitor<TracingOptions> _optionsMonitor;

    public TracingLogProcessor(IOptionsMonitor<TracingOptions> optionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        _optionsMonitor = optionsMonitor;
    }

    public string Process(string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        TelemetrySpan? currentSpan = GetCurrentSpan();

        if (currentSpan != null)
        {
            TracingOptions options = _optionsMonitor.CurrentValue;

            SpanContext context = currentSpan.Context;

            var sb = new StringBuilder(" [");
            sb.Append(options.Name);
            sb.Append(',');

            string traceId = context.TraceId.ToHexString();

            if (traceId.Length > 16 && options.UseShortTraceIds)
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
            sb.Append(message);

            return sb.ToString();
        }

        return message;
    }

    private static TelemetrySpan? GetCurrentSpan()
    {
        TelemetrySpan span = Tracer.CurrentSpan;
        return span.Context.IsValid ? span : null;
    }
}
