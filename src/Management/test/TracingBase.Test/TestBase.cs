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
using System.Reflection;
using Xunit;

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

    protected object GetPrivateField(object baseObject, string fieldName)
        => baseObject.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(baseObject);

    protected void ValidateServiceCollectionCommon(ServiceProvider serviceProvider)
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

    protected void ValidateServiceCollectionBase(ServiceProvider serviceProvider)
    {
        // confirm instrumentation(s) were added as expected
        var tracerProvider = serviceProvider.GetService<TracerProvider>();
        var instrumentations = GetPrivateField(tracerProvider, "instrumentations") as List<object>;
        Assert.NotNull(instrumentations);
        Assert.Single(instrumentations);
        Assert.Contains(instrumentations, obj => obj.GetType().Name.Contains("Http"));

        Assert.IsType<CompositeTextMapPropagator>(Propagators.DefaultTextMapPropagator);
        var comp = Propagators.DefaultTextMapPropagator as CompositeTextMapPropagator;
        var props = GetPrivateField(comp, "propagators") as List<TextMapPropagator>;
        Assert.Equal(2, props.Count);

        // TODO: Investigate alternatives and remove suppression.
#pragma warning disable CS0618 // Type or member is obsolete
        Assert.Contains(props, p => p is B3Propagator);
#pragma warning restore CS0618 // Type or member is obsolete
        Assert.Contains(props, p => p is BaggagePropagator);
    }

    protected void ValidateServiceContainerCore(ServiceProvider serviceProvider)
    {
        var tracerProvider = serviceProvider.GetService<TracerProvider>();

        // confirm instrumentation(s) were added as expected
        var instrumentations = GetPrivateField(tracerProvider, "instrumentations") as List<object>;
        Assert.NotNull(instrumentations);
        Assert.Equal(2, instrumentations.Count);
        Assert.Contains(instrumentations, obj => obj.GetType().Name.Contains("Http"));
        Assert.Contains(instrumentations, obj => obj.GetType().Name.Contains("AspNetCore"));
    }
}
