// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Steeltoe.CircuitBreaker.Hystrix.Configuration;
using Steeltoe.CircuitBreaker.Hystrix.Metric;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Sample;
using Steeltoe.CircuitBreaker.Hystrix.MetricsStream;
using Steeltoe.Common;
using Steeltoe.Connector.Hystrix;

namespace Steeltoe.CircuitBreaker.Hystrix;

public static class HystrixServiceCollectionExtensions
{
    private const string HystrixStreamPrefix = "hystrix:stream";

    public static void AddHystrixMetricsStream(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentGuard.NotNull(services);

        services.AddSingleton(HystrixDashboardStream.GetInstance());
        services.AddHystrixConnection(configuration);
        services.Configure<HystrixMetricsStreamOptions>(configuration.GetSection(HystrixStreamPrefix));
        services.AddSingleton<RabbitMetricsStreamPublisher>();
        services.TryAddSingleton<IHostedService, HystrixMetricStreamService>();
    }

    public static void AddHystrixRequestEventStream(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        services.AddSingleton(HystrixRequestEventsStream.GetInstance());
    }

    public static void AddHystrixUtilizationStream(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        services.AddSingleton(HystrixUtilizationStream.GetInstance());
    }

    public static void AddHystrixConfigStream(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        services.AddSingleton(HystrixConfigurationStream.GetInstance());
    }

    public static void AddHystrixMonitoringStreams(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentGuard.NotNull(services);

        services.AddHystrixMetricsStream(configuration);
        services.AddHystrixConfigStream();
        services.AddHystrixRequestEventStream();
        services.AddHystrixUtilizationStream();
    }
}
