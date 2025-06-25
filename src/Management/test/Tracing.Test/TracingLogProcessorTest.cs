// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry;
using OpenTelemetry.Trace;
using Steeltoe.Common;

namespace Steeltoe.Management.Tracing.Test;

public sealed class TracingLogProcessorTest
{
    [Fact]
    public void Process_NoCurrentSpan_DoesNothing()
    {
        using TracerProvider openTelemetry = Sdk.CreateTracerProviderBuilder().AddSource("tracer-name").Build();
        var processor = new TracingLogProcessor(new ApplicationInstanceInfo());

        string result = processor.Process("InputLogMessage");

        Assert.Equal("InputLogMessage", result);
    }

    [Fact]
    public void Process_CurrentSpan_ReturnsExpected()
    {
        using TracerProvider openTelemetry = Sdk.CreateTracerProviderBuilder().AddSource("tracer-name").Build();

        var processor = new TracingLogProcessor(new ApplicationInstanceInfo
        {
            ApplicationName = "foobar"
        });

        Tracer tracer = TracerProvider.Default.GetTracer("tracer-name");
        TelemetrySpan span = tracer.StartActiveSpan("spanName");

        string result = processor.Process("InputLogMessage");

        Assert.Contains("InputLogMessage", result, StringComparison.Ordinal);
        Assert.Contains('[', result);
        Assert.Contains(']', result);
        Assert.Contains(span.Context.TraceId.ToHexString(), result, StringComparison.Ordinal);
        Assert.Contains(span.Context.SpanId.ToHexString(), result, StringComparison.Ordinal);
        Assert.Contains("foobar", result, StringComparison.Ordinal);

        TelemetrySpan childSpan = tracer.StartActiveSpan("spanName2", SpanKind.Internal, span);

        result = processor.Process("InputLogMessage2");

        Assert.Contains("InputLogMessage2", result, StringComparison.Ordinal);
        Assert.Contains('[', result);
        Assert.Contains(']', result);
        Assert.Contains(childSpan.Context.TraceId.ToHexString(), result, StringComparison.Ordinal);
        Assert.Contains(childSpan.Context.SpanId.ToHexString(), result, StringComparison.Ordinal);

        Assert.Contains("foobar", result, StringComparison.Ordinal);
    }
}
