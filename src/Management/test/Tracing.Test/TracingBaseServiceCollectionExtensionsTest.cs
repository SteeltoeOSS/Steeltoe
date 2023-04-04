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

public class TracingBaseServiceCollectionExtensionsTest : TestBase
{
    [Fact]
    public void AddDistributedTracing_ThrowsOnNulls()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => TracingBaseServiceCollectionExtensions.AddDistributedTracing(null));
        Assert.Equal("services", ex.ParamName);
    }

    [Fact]
    public void AddDistributedTracing_ConfiguresExpectedDefaults()
    {
        IServiceCollection services = new ServiceCollection().AddSingleton(GetConfiguration());
        services.AddLogging();

        ServiceProvider serviceProvider = services.AddDistributedTracing().BuildServiceProvider();

        ValidateServiceCollectionCommon(serviceProvider);
        ValidateServiceCollectionBase(serviceProvider);
    }

    // this test should find Jaeger and Zipkin exporters, see TracingCore.Test for OTLP
    [Fact]
    public void AddDistributedTracing_WiresIncludedExporters()
    {
        IServiceCollection services = new ServiceCollection().AddSingleton(GetConfiguration()).AddLogging();

        ServiceProvider serviceProvider = services.AddDistributedTracing(null).BuildServiceProvider();
        var hst = serviceProvider.GetService<IHostedService>();
        Assert.NotNull(hst);
        var tracerProvider = serviceProvider.GetService<TracerProvider>();
        Assert.NotNull(tracerProvider);

        Assert.NotNull(serviceProvider.GetService<IOptions<ZipkinExporterOptions>>());
        Assert.NotNull(serviceProvider.GetService<IOptions<JaegerExporterOptions>>());
    }

    [Fact]
    public void AddDistributedTracing_ConfiguresSamplers()
    {
        // test AlwaysOn
        IServiceCollection services = new ServiceCollection().AddSingleton(GetConfiguration(new Dictionary<string, string>
        {
            { "Management:Tracing:AlwaysSample", "true" }
        }));

        services.AddLogging();
        ServiceProvider serviceProvider = services.AddDistributedTracing(null).BuildServiceProvider();
        var hst = serviceProvider.GetService<IHostedService>();
        Assert.NotNull(hst);
        var tracerProvider = serviceProvider.GetService<TracerProvider>();
        Assert.NotNull(tracerProvider);

        // test AlwaysOff
        services = new ServiceCollection().AddSingleton(GetConfiguration(new Dictionary<string, string>
        {
            { "Management:Tracing:NeverSample", "true" }
        }));

        serviceProvider = services.AddLogging().AddDistributedTracing(null).BuildServiceProvider();
        hst = serviceProvider.GetService<IHostedService>();
        Assert.NotNull(hst);
        tracerProvider = serviceProvider.GetService<TracerProvider>();
        Assert.NotNull(tracerProvider);
    }
}
