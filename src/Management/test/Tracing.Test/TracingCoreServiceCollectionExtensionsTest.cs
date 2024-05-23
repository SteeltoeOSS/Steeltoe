// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;
using Xunit;

namespace Steeltoe.Management.Tracing.Test;

public sealed class TracingCoreServiceCollectionExtensionsTest : TestBase
{
    [Fact]
    public void AddDistributedTracingAspNetCore_ConfiguresExpectedDefaults()
    {
        IServiceCollection services = new ServiceCollection().AddSingleton(GetConfiguration()).AddLogging();

        ServiceProvider serviceProvider = services.AddDistributedTracingAspNetCore().BuildServiceProvider(true);

        ValidateServiceCollectionCommon(serviceProvider);
        ValidateServiceContainerCore(serviceProvider);
    }

    [Fact]
    public void AddDistributedTracingAspNetCore_WiresIncludedExporters()
    {
        IServiceCollection services = new ServiceCollection().AddSingleton(GetConfiguration()).AddLogging();

        ServiceProvider serviceProvider = services.AddDistributedTracing(null).BuildServiceProvider(true);
        var hst = serviceProvider.GetService<IHostedService>();
        Assert.NotNull(hst);
        var tracerProvider = serviceProvider.GetService<TracerProvider>();
        Assert.NotNull(tracerProvider);

        Assert.NotNull(serviceProvider.GetService<IOptions<OtlpExporterOptions>>());
    }

    [Fact]
    public void AddDistributedTracingAspNetCore_WiresWavefrontExporters()
    {
        IServiceCollection services = new ServiceCollection().AddSingleton(GetConfiguration(new Dictionary<string, string?>
        {
            { "management:metrics:export:wavefront:uri", "https://test.wavefront.com" },
            { "management:metrics:export:wavefront:apiToken", "fakeSecret" }
        }));

        ServiceProvider serviceProvider = services.AddLogging().AddDistributedTracing(null).BuildServiceProvider(true);

        var tracerProvider = serviceProvider.GetService<TracerProvider>();
        Assert.NotNull(tracerProvider);
    }
}
