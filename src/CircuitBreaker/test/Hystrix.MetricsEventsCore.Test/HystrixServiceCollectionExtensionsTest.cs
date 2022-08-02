// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.CircuitBreaker.Hystrix.Config;
using Steeltoe.CircuitBreaker.Hystrix.Metric;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Sample;
using Steeltoe.CircuitBreaker.Hystrix.MetricsEventsCore.EventSources;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsEvents.Test;

public class HystrixServiceCollectionExtensionsTest
{
    [Fact]
    public void AddHystrixStreams_ThrowsIfServiceContainerNull()
    {
        const IServiceCollection services = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddHystrixConfigStream());
        Assert.Contains(nameof(services), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixMetricsStream(null));
        Assert.Contains(nameof(services), ex2.Message);

        var ex3 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixMonitoringStreams(null));
        Assert.Contains(nameof(services), ex3.Message);

        var ex4 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixRequestEventStream());
        Assert.Contains(nameof(services), ex4.Message);
    }

    [Fact]
    public void AddHystrixMetricsEventSource_ThrowsIfServiceContainerNull()
    {
        const IServiceCollection services = null;
        var ex5 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixMetricsEventSource());
        Assert.Contains(nameof(services), ex5.Message);
    }

    [Fact]
    public void AddHystrixMetricsStream_AddsStream()
    {
        IServiceCollection services = new ServiceCollection();

        services.AddHystrixMetricsStream(null);

        Assert.NotNull(services.BuildServiceProvider().GetService<HystrixDashboardStream>());
    }

    [Fact]
    public void AddHystrixMetricsEventSource_AddsHostedService()
    {
        IServiceCollection services = new ServiceCollection();

        services.AddHystrixMetricsEventSource();

        Assert.IsType<HystrixEventSourceService>(services.BuildServiceProvider().GetService<IHostedService>());
    }

    [Fact]
    public void AddHystrixRequestEventStream_AddsStream()
    {
        IServiceCollection services = new ServiceCollection();

        services.AddHystrixRequestEventStream();

        Assert.NotNull(services.BuildServiceProvider().GetService<HystrixRequestEventsStream>());
    }

    [Fact]
    public void AddHystrixUtilizationStream_AddsStream()
    {
        IServiceCollection services = new ServiceCollection();

        services.AddHystrixUtilizationStream();

        Assert.NotNull(services.BuildServiceProvider().GetService<HystrixUtilizationStream>());
    }

    [Fact]
    public void AddHystrixConfigStream_AddsStream()
    {
        IServiceCollection services = new ServiceCollection();

        services.AddHystrixConfigStream();

        Assert.NotNull(services.BuildServiceProvider().GetService<HystrixConfigurationStream>());
    }

    [Fact]
    public void AddHystrixMonitoringStreams_AddsAllStreams()
    {
        IServiceCollection services = new ServiceCollection();

        services.AddHystrixMonitoringStreams(null);
        ServiceProvider sp = services.BuildServiceProvider();

        Assert.NotNull(sp.GetService<HystrixDashboardStream>());
        Assert.NotNull(sp.GetService<HystrixRequestEventsStream>());
        Assert.NotNull(sp.GetService<HystrixUtilizationStream>());
        Assert.NotNull(sp.GetService<HystrixConfigurationStream>());
    }
}
