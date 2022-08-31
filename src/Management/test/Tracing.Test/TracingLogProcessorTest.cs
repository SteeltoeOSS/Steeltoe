// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Steeltoe.Common;
using Xunit;

namespace Steeltoe.Management.Tracing.Test;

public class TracingLogProcessorTest
{
    [Fact]
    public void Process_NoCurrentSpan_DoesNothing()
    {
        using TracerProvider openTelemetry = Sdk.CreateTracerProviderBuilder().AddSource("tracername").Build();
        var opts = new TracingOptions(null, new ConfigurationBuilder().Build());
        var processor = new TracingLogProcessor(opts);

        string result = processor.Process("InputLogMessage");

        Assert.Equal("InputLogMessage", result);
    }

    [Fact]
    public void Process_CurrentSpan_ReturnsExpected()
    {
        using TracerProvider openTelemetry = Sdk.CreateTracerProviderBuilder().AddSource("tracername").Build();

        IConfiguration configuration = TestHelpers.GetConfigurationFromDictionary(new Dictionary<string, string>
        {
            ["management:tracing:name"] = "foobar"
        });

        var processor = new TracingLogProcessor(new TracingOptions(new ApplicationInstanceInfo(configuration), configuration));
        Tracer tracer = TracerProvider.Default.GetTracer("tracername");
        TelemetrySpan span = tracer.StartActiveSpan("spanName");

        string result = processor.Process("InputLogMessage");

        Assert.Contains("InputLogMessage", result);
        Assert.Contains("[", result);
        Assert.Contains("]", result);
        Assert.Contains(span.Context.TraceId.ToHexString(), result);
        Assert.Contains(span.Context.SpanId.ToHexString(), result);
        Assert.Contains("foobar", result);

        TelemetrySpan childSpan = tracer.StartActiveSpan("spanName2", SpanKind.Internal, span);

        result = processor.Process("InputLogMessage2");

        Assert.Contains("InputLogMessage2", result);
        Assert.Contains("[", result);
        Assert.Contains("]", result);
        Assert.Contains(childSpan.Context.TraceId.ToHexString(), result);
        Assert.Contains(childSpan.Context.SpanId.ToHexString(), result);

        // Assert.Contains(span.Context.SpanId.ToHexString(), result);  TODO: ParentID not supported
        Assert.Contains("foobar", result);
    }

    [Fact]
    public void Process_UseShortTraceIds()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["management:tracing:name"] = "foobar",
            ["management:tracing:useShortTraceIds"] = "true"
        };

        IConfiguration configuration = TestHelpers.GetConfigurationFromDictionary(appsettings);
        var opts = new TracingOptions(new ApplicationInstanceInfo(configuration), configuration);

        using TracerProvider openTelemetry = Sdk.CreateTracerProviderBuilder().AddSource("tracername").Build();
        Tracer tracer = TracerProvider.Default.GetTracer("tracername");
        TelemetrySpan span = tracer.StartActiveSpan("spanName");
        var processor = new TracingLogProcessor(opts);

        string result = processor.Process("InputLogMessage");

        Assert.Contains("InputLogMessage", result);
        Assert.Contains("[", result);
        Assert.Contains("]", result);

        string full = span.Context.TraceId.ToHexString();
        string shorty = full.Substring(full.Length - 16, 16);

        Assert.Contains(shorty, result);
        Assert.DoesNotContain(full, result);

        Assert.Contains(span.Context.SpanId.ToHexString(), result);
        Assert.Contains("foobar", result);
    }
}
