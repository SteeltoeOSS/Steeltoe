// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Diagnostics;
using Steeltoe.Management.Endpoint.Metrics;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Metrics;

public class EndpointServiceCollectionExtensionsTest : BaseTest
{
    [Fact]
    public void AddMetricsActuator_ThrowsOnNulls()
    {
        const IServiceCollection services = null;
        IServiceCollection services2 = new ServiceCollection();

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddMetricsActuator());
        Assert.Contains(nameof(services), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddMetricsActuator_AddsCorrectServices()
    {
        var services = new ServiceCollection();
        IConfiguration configuration = GetConfiguration();

        services.AddOptions();
        services.AddLogging();
        services.AddSingleton(HostingHelpers.GetHostingEnvironment());
        services.AddSingleton(configuration);
        services.AddMetricsActuator();

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        var mgr = serviceProvider.GetService<IDiagnosticsManager>();
        Assert.NotNull(mgr);
        var hst = serviceProvider.GetService<IHostedService>();
        Assert.NotNull(hst);
        var opts = serviceProvider.GetService<IOptionsMonitor<MetricsObserverOptions>>();
        Assert.NotNull(opts.CurrentValue);

        IEnumerable<IDiagnosticObserver> observers = serviceProvider.GetServices<IDiagnosticObserver>();
        List<IDiagnosticObserver> list = observers.ToList();
        Assert.NotEmpty(list); // Changed how this works Todo: add a new test to verify functionality

        var ep = serviceProvider.GetService<IMetricsEndpoint>();
        Assert.NotNull(ep);
    }

    private IConfiguration GetConfiguration()
    {
        var builder = new ConfigurationBuilder();
        return builder.Build();
    }
}
