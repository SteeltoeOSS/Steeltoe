// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;

namespace Steeltoe.Management.Tracing.Test;

public sealed class TracingCoreServiceCollectionExtensionsTest : TestBase
{
    [Fact]
    public async Task AddDistributedTracingAspNetCore_ConfiguresExpectedDefaults()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(GetConfiguration());
        services.AddLogging();
        services.AddDistributedTracingAspNetCore();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        ValidateServiceCollectionCommon(serviceProvider);
        ValidateServiceContainerCore(serviceProvider);
    }

    [Fact]
    public async Task AddDistributedTracingAspNetCore_WiresIncludedExporters()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(GetConfiguration());
        services.AddLogging();
        services.AddDistributedTracing(null);
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        IHostedService[] hostedServices = serviceProvider.GetServices<IHostedService>().ToArray();
        Assert.Single(hostedServices, hostedService => hostedService.GetType().Name == "TelemetryHostedService");

        var tracerProvider = serviceProvider.GetService<TracerProvider>();
        Assert.NotNull(tracerProvider);

        var otlpOptions = serviceProvider.GetRequiredService<IOptions<OtlpExporterOptions>>();
        Assert.NotNull(otlpOptions.Value.Endpoint);
    }
}
