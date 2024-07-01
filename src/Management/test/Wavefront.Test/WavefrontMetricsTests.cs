// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Diagnostics;
using Steeltoe.Management.Endpoint.Diagnostics;
using Steeltoe.Management.Wavefront.Exporters;

namespace Steeltoe.Management.Wavefront.Test;

public sealed class WavefrontMetricsTests
{
    [Fact]
    public async Task AddWavefront_WebApplicationBuilder()
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "management:metrics:export:wavefront:apiToken", "test" },
            { "management:metrics:export:wavefront:uri", "http://test.io" },
            { "management:metrics:export:wavefront:step", "500" }
        };

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddWavefrontMetrics();
        builder.WebHost.UseTestServer();

        await using WebApplication host = builder.Build();
        await host.StartAsync();

        AssertExporterIsAdded(host.Services);
    }

    [Fact]
    public async Task AddWavefront_IWebHostBuilder()
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "management:metrics:export:wavefront:uri", "https://wavefront.vmware.com" },
            { "management:metrics:export:wavefront:apiToken", "testToken" }
        };

        IWebHostBuilder hostBuilder = new WebHostBuilder();
        hostBuilder.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        hostBuilder.Configure(HostingHelpers.EmptyAction);
        hostBuilder.ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(appSettings));
        hostBuilder.ConfigureServices(services => services.AddWavefrontMetrics());

        using IWebHost host = hostBuilder.UseTestServer().Build();
        await host.StartAsync();

        IEnumerable<IDiagnosticsManager> diagnosticsManagers = host.Services.GetServices<IDiagnosticsManager>();
        diagnosticsManagers.Should().HaveCount(1);

        IEnumerable<DiagnosticsService> diagnosticServices = host.Services.GetServices<IHostedService>().OfType<DiagnosticsService>();
        diagnosticServices.Should().HaveCount(1);

        var optionsMonitor = host.Services.GetRequiredService<IOptionsMonitor<WavefrontExporterOptions>>();
        optionsMonitor.CurrentValue.ApiToken.Should().Be("testToken");

        AssertExporterIsAdded(host.Services);
    }

    [Fact]
    public void AddWavefront_ProxyConfigIsValid()
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "management:metrics:export:wavefront:uri", "proxy://wavefront.vmware.com" },
            { "management:metrics:export:wavefront:apiToken", string.Empty } // Should not throw
        };

        IWebHostBuilder hostBuilder = new WebHostBuilder();
        hostBuilder.Configure(HostingHelpers.EmptyAction);
        hostBuilder.ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(appSettings));
        hostBuilder.ConfigureServices(services => services.AddWavefrontMetrics());

        using IWebHost host = hostBuilder.Build();
        host.Should().NotBeNull();
    }

    private static void AssertExporterIsAdded(IServiceProvider services)
    {
        var meterProvider = services.GetService<MeterProvider>();
        meterProvider.Should().NotBeNull();

        object? reader = GetProperty(meterProvider!, "Reader");
        reader.Should().NotBeNull();
        reader.Should().BeOfType<PeriodicExportingMetricReader>();

        object? exporter = GetProperty(reader!, "Exporter");
        exporter.Should().NotBeNull();
        exporter.Should().BeOfType<WavefrontMetricsExporter>();
    }

    private static object? GetProperty(object instance, string propertyName)
    {
        Type type = instance.GetType();
        PropertyInfo? property = type.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Instance);
        property.Should().NotBeNull();

        return property!.GetValue(instance);
    }
}
