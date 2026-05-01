// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;
using B3Propagator = OpenTelemetry.Extensions.Propagators.B3Propagator;

namespace Steeltoe.Management.Tracing.Test;

public class TestBase
{
    public virtual TracingOptions GetOptions()
    {
        var opts = new TracingOptions(null, GetConfiguration());
        return opts;
    }

    public virtual IConfiguration GetConfiguration() =>
        GetConfiguration(new Dictionary<string, string>());

    public virtual IConfiguration GetConfiguration(Dictionary<string, string> moreSettings)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>(moreSettings) { { "management:tracing:name", "foobar" } })
            .Build();

    protected TelemetrySpan GetCurrentSpan(Tracer tracer)
    {
        var span = Tracer.CurrentSpan;
        return span.Context.IsValid ? span : null;
    }

    protected void ValidateServiceCollectionCommon(IServiceProvider serviceProvider)
    {
        // confirm Steeltoe types were registered
        Assert.NotNull(serviceProvider.GetService<ITracingOptions>());
        Assert.IsType<TracingLogProcessor>(serviceProvider.GetService<IDynamicMessageProcessor>());

        // confirm OpenTelemetry types were registered
        var tracerProvider = serviceProvider.GetService<TracerProvider>();
        Assert.NotNull(tracerProvider);
        var hst = serviceProvider.GetService<IHostedService>();
        Assert.NotNull(hst);
    }

    protected void ValidateServiceCollectionBase(IServiceProvider serviceProvider)
    {
        // confirm instrumentation(s) were added as expected
        var tracerProvider = serviceProvider.GetService<TracerProvider>();
        var instrumentations = GetNonPublicTracerProviderInstrumentations(tracerProvider);
        Assert.NotNull(instrumentations);
        Assert.Single(instrumentations);
        Assert.Contains(instrumentations, obj => obj.GetType().Name.Contains("Http"));

        Assert.IsType<CompositeTextMapPropagator>(Propagators.DefaultTextMapPropagator);
        var comp = Propagators.DefaultTextMapPropagator as CompositeTextMapPropagator;
        var props = GetNonPublicCompositePropagators(comp);
        Assert.NotNull(props);
        Assert.Equal(2, props.Count);
        Assert.Contains(props, p => p is B3Propagator);
        Assert.Contains(props, p => p is BaggagePropagator);
    }

    protected void ValidateServiceContainerCore(IServiceProvider serviceProvider)
    {
        var tracerProvider = serviceProvider.GetService<TracerProvider>();

        // confirm instrumentation(s) were added as expected
        var instrumentations = GetNonPublicTracerProviderInstrumentations(tracerProvider);
        Assert.NotNull(instrumentations);
        Assert.Equal(2, instrumentations.Count);
        Assert.Contains(instrumentations, obj => obj.GetType().Name.Contains("Http"));
        Assert.Contains(instrumentations, obj => obj.GetType().Name.Contains("AspNetCore"));
    }

    private static List<object> GetNonPublicTracerProviderInstrumentations(TracerProvider tracerProvider)
    {
        var property = tracerProvider?.GetType().GetProperty(
            "Instrumentations",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        return property?.GetValue(tracerProvider) as List<object>;
    }

    private static List<TextMapPropagator> GetNonPublicCompositePropagators(CompositeTextMapPropagator composite)
    {
        var field = composite?.GetType().GetField(
            "propagators",
            BindingFlags.NonPublic | BindingFlags.Instance);

        return field?.GetValue(composite) as List<TextMapPropagator>;
    }
}