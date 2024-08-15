// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using Steeltoe.Logging;
using B3Propagator = OpenTelemetry.Extensions.Propagators.B3Propagator;

namespace Steeltoe.Management.Tracing.Test;

public class TestBase
{
    protected IConfiguration GetConfiguration()
    {
        return GetConfiguration(new Dictionary<string, string?>());
    }

    protected IConfiguration GetConfiguration(Dictionary<string, string?> moreSettings)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(moreSettings)
        {
            { "management:tracing:name", "foobar" }
        }).Build();
    }

    private object? GetPrivateField(object baseObject, string fieldName)
    {
        return baseObject.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(baseObject);
    }

    protected void ValidateServiceCollectionCommon(IServiceProvider serviceProvider)
    {
        // confirm Steeltoe types were registered
        TracingOptions tracingOptions = serviceProvider.GetRequiredService<IOptions<TracingOptions>>().Value;
        tracingOptions.Name.Should().NotBeNull();

        Assert.IsType<TracingLogProcessor>(serviceProvider.GetRequiredService<IDynamicMessageProcessor>());

        // confirm OpenTelemetry types were registered
        var tracerProvider = serviceProvider.GetService<TracerProvider>();
        Assert.NotNull(tracerProvider);
        IHostedService[] hostedServices = serviceProvider.GetServices<IHostedService>().ToArray();
        Assert.Single(hostedServices, hostedService => hostedService.GetType().Name == "TelemetryHostedService");
    }

    protected void ValidateServiceCollectionBase(IServiceProvider serviceProvider)
    {
        // confirm instrumentation(s) were added as expected
        var tracerProvider = serviceProvider.GetRequiredService<TracerProvider>();
        var instrumentations = GetPrivateField(tracerProvider, "instrumentations") as List<object>;
        Assert.NotNull(instrumentations);
        Assert.Single(instrumentations);
        Assert.Contains(instrumentations, obj => obj.GetType().Name.Contains("Http", StringComparison.Ordinal));

        Assert.IsType<CompositeTextMapPropagator>(Propagators.DefaultTextMapPropagator);
        var composite = (CompositeTextMapPropagator)Propagators.DefaultTextMapPropagator;
        var propagators = GetPrivateField(composite, "propagators") as List<TextMapPropagator>;
        Assert.NotNull(propagators);
        Assert.Equal(2, propagators.Count);

        Assert.Contains(propagators, propagator => propagator is B3Propagator);
        Assert.Contains(propagators, propagator => propagator is BaggagePropagator);
    }

    protected void ValidateServiceContainerCore(IServiceProvider serviceProvider)
    {
        var tracerProvider = serviceProvider.GetRequiredService<TracerProvider>();

        // confirm instrumentation(s) were added as expected
        var instrumentations = GetPrivateField(tracerProvider, "instrumentations") as List<object>;
        Assert.NotNull(instrumentations);
        Assert.Equal(2, instrumentations.Count);
        Assert.Contains(instrumentations, obj => obj.GetType().Name.Contains("Http", StringComparison.Ordinal));
        Assert.Contains(instrumentations, obj => obj.GetType().Name.Contains("AspNetCore", StringComparison.Ordinal));
    }
}
