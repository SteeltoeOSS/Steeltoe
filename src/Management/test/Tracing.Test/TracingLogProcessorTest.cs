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

        result.Should().Be("InputLogMessage");
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

        string message1 = processor.Process("InputLogMessage1");

        message1.Should().Be($" [foobar,{span.Context.TraceId},{span.Context.SpanId},0000000000000000,true] InputLogMessage1");

        TelemetrySpan childSpan = tracer.StartActiveSpan("spanName2", SpanKind.Internal, span);

        string message2 = processor.Process("InputLogMessage2");

        message2.Should().Be($" [foobar,{childSpan.Context.TraceId},{childSpan.Context.SpanId},{span.Context.SpanId},true] InputLogMessage2");
    }
}
