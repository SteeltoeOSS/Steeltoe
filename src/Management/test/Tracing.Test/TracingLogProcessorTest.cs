// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Steeltoe.Common;
using Steeltoe.Common.Extensions;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Management.Tracing.Test;

public sealed class TracingLogProcessorTest
{
    [Fact]
    public void Process_NoCurrentSpan_DoesNothing()
    {
        using TracerProvider openTelemetry = Sdk.CreateTracerProviderBuilder().AddSource("tracername").Build();
        var optionsMonitor = new TestOptionsMonitor<TracingOptions>();
        var processor = new TracingLogProcessor(optionsMonitor);

        string result = processor.Process("InputLogMessage");

        Assert.Equal("InputLogMessage", result);
    }

    [Fact]
    public void Process_CurrentSpan_ReturnsExpected()
    {
        using TracerProvider openTelemetry = Sdk.CreateTracerProviderBuilder().AddSource("tracername").Build();

        var appSettings = new Dictionary<string, string?>
        {
            ["management:tracing:name"] = "foobar"
        };

        TestOptionsMonitor<TracingOptions> optionsMonitor = GetTracingOptionsMonitor(appSettings);
        var processor = new TracingLogProcessor(optionsMonitor);
        Tracer tracer = TracerProvider.Default.GetTracer("tracername");
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

    [Fact]
    public void Process_UseShortTraceIds()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:tracing:name"] = "foobar",
            ["management:tracing:useShortTraceIds"] = "true"
        };

        TestOptionsMonitor<TracingOptions> optionsMonitor = GetTracingOptionsMonitor(appSettings);
        using TracerProvider openTelemetry = Sdk.CreateTracerProviderBuilder().AddSource("tracername").Build();
        Tracer tracer = TracerProvider.Default.GetTracer("tracername");
        TelemetrySpan span = tracer.StartActiveSpan("spanName");
        var processor = new TracingLogProcessor(optionsMonitor);

        string result = processor.Process("InputLogMessage");

        Assert.Contains("InputLogMessage", result, StringComparison.Ordinal);
        Assert.Contains('[', result);
        Assert.Contains(']', result);

        string full = span.Context.TraceId.ToHexString();
        string shorty = full.Substring(full.Length - 16, 16);

        Assert.Contains(shorty, result, StringComparison.Ordinal);
        Assert.DoesNotContain(full, result, StringComparison.Ordinal);

        Assert.Contains(span.Context.SpanId.ToHexString(), result, StringComparison.Ordinal);
        Assert.Contains("foobar", result, StringComparison.Ordinal);
    }

    private TestOptionsMonitor<TracingOptions> GetTracingOptionsMonitor(IDictionary<string, string?> appSettings)
    {
        var options = new TracingOptions();

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = builder.Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddApplicationInstanceInfo();
        using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var appInfo = serviceProvider.GetRequiredService<IApplicationInstanceInfo>();
        var configurer = new ConfigureTracingOptions(configuration, appInfo);
        configurer.Configure(options);

        return TestOptionsMonitor.Create(options);
    }
}
