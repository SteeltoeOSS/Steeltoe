// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Steeltoe.Common;
using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Management.Tracing.Test;

public sealed class TracingLogProcessorTest
{
    [Fact]
    public void Process_NoCurrentSpan_DoesNothing()
    {
        using TracerProvider? openTelemetry = Sdk.CreateTracerProviderBuilder().AddSource("tracername").Build();
        var options = new TracingOptions(null, new ConfigurationBuilder().Build());
        var processor = new TracingLogProcessor(options);

        string result = processor.Process("InputLogMessage");

        Assert.Equal("InputLogMessage", result);
    }

    [Fact]
    public void Process_CurrentSpan_ReturnsExpected()
    {
        using TracerProvider? openTelemetry = Sdk.CreateTracerProviderBuilder().AddSource("tracername").Build();

        IConfiguration configuration = TestHelpers.GetConfigurationFromDictionary(new Dictionary<string, string?>
        {
            ["management:tracing:name"] = "foobar"
        });

        var processor = new TracingLogProcessor(new TracingOptions(new ApplicationInstanceInfo(configuration), configuration));
        Tracer tracer = TracerProvider.Default.GetTracer("tracername");
        TelemetrySpan span = tracer.StartActiveSpan("spanName");

        string result = processor.Process("InputLogMessage");

        Assert.Contains("InputLogMessage", result, StringComparison.Ordinal);
        Assert.Contains("[", result, StringComparison.Ordinal);
        Assert.Contains("]", result, StringComparison.Ordinal);
        Assert.Contains(span.Context.TraceId.ToHexString(), result, StringComparison.Ordinal);
        Assert.Contains(span.Context.SpanId.ToHexString(), result, StringComparison.Ordinal);
        Assert.Contains("foobar", result, StringComparison.Ordinal);

        TelemetrySpan childSpan = tracer.StartActiveSpan("spanName2", SpanKind.Internal, span);

        result = processor.Process("InputLogMessage2");

        Assert.Contains("InputLogMessage2", result, StringComparison.Ordinal);
        Assert.Contains("[", result, StringComparison.Ordinal);
        Assert.Contains("]", result, StringComparison.Ordinal);
        Assert.Contains(childSpan.Context.TraceId.ToHexString(), result, StringComparison.Ordinal);
        Assert.Contains(childSpan.Context.SpanId.ToHexString(), result, StringComparison.Ordinal);

        Assert.Contains("foobar", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Process_UseShortTraceIds()
    {
        var appsettings = new Dictionary<string, string?>
        {
            ["management:tracing:name"] = "foobar",
            ["management:tracing:useShortTraceIds"] = "true"
        };

        IConfiguration configuration = TestHelpers.GetConfigurationFromDictionary(appsettings);
        var options = new TracingOptions(new ApplicationInstanceInfo(configuration), configuration);

        using TracerProvider? openTelemetry = Sdk.CreateTracerProviderBuilder().AddSource("tracername").Build();
        Tracer tracer = TracerProvider.Default.GetTracer("tracername");
        TelemetrySpan span = tracer.StartActiveSpan("spanName");
        var processor = new TracingLogProcessor(options);

        string result = processor.Process("InputLogMessage");

        Assert.Contains("InputLogMessage", result, StringComparison.Ordinal);
        Assert.Contains("[", result, StringComparison.Ordinal);
        Assert.Contains("]", result, StringComparison.Ordinal);

        string full = span.Context.TraceId.ToHexString();
        string shorty = full.Substring(full.Length - 16, 16);

        Assert.Contains(shorty, result, StringComparison.Ordinal);
        Assert.DoesNotContain(full, result, StringComparison.Ordinal);

        Assert.Contains(span.Context.SpanId.ToHexString(), result, StringComparison.Ordinal);
        Assert.Contains("foobar", result, StringComparison.Ordinal);
    }
}
