// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;

namespace Steeltoe.Management.Tracing.Test;

public sealed class TracingBaseServiceCollectionExtensionsTest : TestBase
{
    [Fact]
    public void AddDistributedTracing_ConfiguresExpectedDefaults()
    {
        IServiceCollection services = new ServiceCollection().AddSingleton(GetConfiguration());
        services.AddLogging();

        ServiceProvider serviceProvider = services.AddDistributedTracing().BuildServiceProvider(true);

        ValidateServiceCollectionCommon(serviceProvider);
        ValidateServiceCollectionBase(serviceProvider);
    }

    [Fact]
    public void AddDistributedTracing_WiresIncludedExporters()
    {
        IServiceCollection services = new ServiceCollection().AddSingleton(GetConfiguration()).AddLogging();

        ServiceProvider serviceProvider = services.AddDistributedTracing(null).BuildServiceProvider(true);

        IHostedService[] hostedServices = serviceProvider.GetServices<IHostedService>().ToArray();
        Assert.Single(hostedServices, hostedService => hostedService.GetType().Name == "TelemetryHostedService");

        var tracerProvider = serviceProvider.GetService<TracerProvider>();
        Assert.NotNull(tracerProvider);

        var zipkinOptions = serviceProvider.GetRequiredService<IOptions<ZipkinExporterOptions>>();
        Assert.NotNull(zipkinOptions.Value.Endpoint);

        var jaegerOptions = serviceProvider.GetRequiredService<IOptions<JaegerExporterOptions>>();
        Assert.NotNull(jaegerOptions.Value.Endpoint);
    }

    [Fact]
    public void AddDistributedTracing_ConfiguresSamplers()
    {
        // test AlwaysOn
        IServiceCollection services = new ServiceCollection().AddSingleton(GetConfiguration(new Dictionary<string, string?>
        {
            { "Management:Tracing:AlwaysSample", "true" }
        }));

        services.AddLogging();
        ServiceProvider serviceProvider = services.AddDistributedTracing(null).BuildServiceProvider(true);

        IHostedService[] hostedServices = serviceProvider.GetServices<IHostedService>().ToArray();
        Assert.Single(hostedServices, hostedService => hostedService.GetType().Name == "TelemetryHostedService");

        var tracerProvider = serviceProvider.GetService<TracerProvider>();
        Assert.NotNull(tracerProvider);

        // test AlwaysOff
        services = new ServiceCollection().AddSingleton(GetConfiguration(new Dictionary<string, string?>
        {
            { "Management:Tracing:NeverSample", "true" }
        }));

        serviceProvider = services.AddLogging().AddDistributedTracing(null).BuildServiceProvider(true);

        hostedServices = serviceProvider.GetServices<IHostedService>().ToArray();
        Assert.Single(hostedServices, hostedService => hostedService.GetType().Name == "TelemetryHostedService");

        tracerProvider = serviceProvider.GetService<TracerProvider>();
        Assert.NotNull(tracerProvider);
    }
}
