// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.CircuitBreaker.Hystrix.Config;
using Steeltoe.CircuitBreaker.Hystrix.Metric;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Sample;
using Steeltoe.CircuitBreaker.Hystrix.MetricsEventsCore.EventSources;
using System;
using System.Diagnostics.Tracing;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public static class HystrixServiceCollectionExtensions
    {
        public static void AddHystrixMetricsStream(this IServiceCollection services, IConfiguration config)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<HystrixDashboardStream>(HystrixDashboardStream.GetInstance());
        }

        public static void AddHystrixMetricsEventSource(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddHostedService<HystrixEventSourceService>();
        }

        public static void AddHystrixRequestEventStream(this IServiceCollection services, IConfiguration config)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<HystrixRequestEventsStream>(HystrixRequestEventsStream.GetInstance());
        }

        public static void AddHystrixUtilizationStream(this IServiceCollection services, IConfiguration config)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<HystrixUtilizationStream>(HystrixUtilizationStream.GetInstance());
        }

        public static void AddHystrixConfigStream(this IServiceCollection services, IConfiguration config)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<HystrixConfigurationStream>(HystrixConfigurationStream.GetInstance());
        }

        public static void AddHystrixMonitoringStreams(this IServiceCollection services, IConfiguration config)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddHystrixMetricsStream(config);
            services.AddHystrixConfigStream(config);
            services.AddHystrixRequestEventStream(config);
            services.AddHystrixUtilizationStream(config);
        }
    }
}
