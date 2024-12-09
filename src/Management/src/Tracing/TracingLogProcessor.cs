// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using OpenTelemetry.Trace;
using Steeltoe.Common;
using Steeltoe.Logging;

namespace Steeltoe.Management.Tracing;

/// <summary>
/// An <see cref="IDynamicMessageProcessor" /> that adds details of <see cref="Tracer.CurrentSpan" /> (if found) to log messages.
/// </summary>
public sealed class TracingLogProcessor : IDynamicMessageProcessor
{
    private readonly IApplicationInstanceInfo _applicationInstanceInfo;

    public TracingLogProcessor(IApplicationInstanceInfo applicationInstanceInfo)
    {
        ArgumentNullException.ThrowIfNull(applicationInstanceInfo);

        _applicationInstanceInfo = applicationInstanceInfo;
    }

    public string Process(string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        TelemetrySpan? currentSpan = GetCurrentSpan();

        if (currentSpan == null)
        {
            return message;
        }

        SpanContext context = currentSpan.Context;

        var sb = new StringBuilder(" [");
        sb.Append(_applicationInstanceInfo.ApplicationName);
        sb.Append(',');

        string traceId = context.TraceId.ToHexString();

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

    private static TelemetrySpan? GetCurrentSpan()
    {
        TelemetrySpan span = Tracer.CurrentSpan;
        return span.Context.IsValid ? span : null;
    }
}
