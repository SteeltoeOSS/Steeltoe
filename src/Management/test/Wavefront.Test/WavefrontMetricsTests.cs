// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Steeltoe.Management.Diagnostics;
using Steeltoe.Management.Endpoint.Diagnostics;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Wavefront.Exporters;
using Xunit;

namespace Steeltoe.Management.Wavefront.Test;
public class WavefrontMetricsTests
{


    [Fact]
    public async Task AddWavefrontExporter()
    {
        var settings = new Dictionary<string, string>
        {
            { "management:metrics:export:wavefront:apiToken", "test" },
            { "management:metrics:export:wavefront:uri", "http://test.io" },
            { "management:metrics:export:wavefront:step", "500" }
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(settings);
        builder.WebHost.UseTestServer();

        using WebApplication host = builder.AddWavefrontMetrics().Build();

        await host.StartAsync();
        AssertExporterIsAdded(host.Services);
        await host.StopAsync();
    }

    [Fact]
    public async Task AddWavefront_IWebHostBuilder()
    {
        var wfSettings = new Dictionary<string, string>
        {
            { "management:metrics:export:wavefront:uri", "https://wavefront.vmware.com" },
            { "management:metrics:export:wavefront:apiToken", "testToken" }
        };

        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        }).ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(wfSettings));
        using IWebHost host = hostBuilder.UseTestServer()
            .AddWavefrontMetrics().Build();

        await host.StartAsync();

        IEnumerable<IDiagnosticsManager> diagnosticsManagers = host.Services.GetServices<IDiagnosticsManager>();
        Assert.Single(diagnosticsManagers);
        IEnumerable<DiagnosticServices> diagnosticServices = host.Services.GetServices<IHostedService>().OfType<DiagnosticServices>();
        Assert.Single(diagnosticServices);
        IEnumerable<IMetricsObserverOptions> options = host.Services.GetServices<IMetricsObserverOptions>();
        Assert.Single(options);

        AssertExporterIsAdded(host.Services);
        await host.StopAsync();
    }

    private static void AssertExporterIsAdded(IServiceProvider services)
    {
        var meterProvider = services.GetService<MeterProvider>();
        Assert.NotNull(meterProvider);
        object reader = GetProperty(meterProvider, "Reader");
        Assert.NotNull(reader);
        Assert.IsType<PeriodicExportingMetricReader>(reader);


        object exporter = GetProperty(reader, "Exporter");
        Assert.NotNull(exporter);
        Assert.IsType<WavefrontMetricsExporter>(exporter);
    }

    private static object GetProperty(object meterProvider, string propName)
    {
        return meterProvider.GetType().GetProperty(propName, BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(meterProvider);
    }

    [Fact]
    public void AddWavefront_ProxyConfigIsValid()
    {
        var wfSettings = new Dictionary<string, string>
        {
            { "management:metrics:export:wavefront:uri", "proxy://wavefront.vmware.com" },
            { "management:metrics:export:wavefront:apiToken", string.Empty } // Should not throw
        };

        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        }).ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(wfSettings));

        IWebHost host = hostBuilder.AddWavefrontMetrics().Build();
        Assert.NotNull(host);

    }


    [Fact]
    public void AddWavefront_ThrowsWhenNull()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => WavefrontExtensions.AddWavefrontMetrics((IServiceCollection)null));
        Assert.Contains("services", ex.Message, StringComparison.Ordinal);
    }
}
