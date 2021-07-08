// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.CircuitBreaker.Hystrix.Config;
using Steeltoe.CircuitBreaker.Hystrix.Metric;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Sample;
using Steeltoe.CircuitBreaker.Hystrix.MetricsEventsCore.EventSources;
using System;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public static class HystrixServiceCollectionExtensions
    {
        [Obsolete("The IConfiguration parameter is not used, this method signature will be removed in a future release")]
        public static void AddHystrixMetricsStream(this IServiceCollection services, IConfiguration config)
            => AddHystrixMetricsStream(services);

        public static void AddHystrixMetricsStream(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddSingleton(HystrixDashboardStream.GetInstance());
        }

        public static void AddHystrixMetricsEventSource(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddHystrixMetricsStream();
            services.AddHostedService<HystrixEventSourceService>();
        }

        [Obsolete("The IConfiguration parameter is not used, this method signature will be removed in a future release")]
        public static void AddHystrixRequestEventStream(this IServiceCollection services, IConfiguration config)
            => AddHystrixRequestEventStream(services);

        public static void AddHystrixRequestEventStream(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton(HystrixRequestEventsStream.GetInstance());
        }

        [Obsolete("The IConfiguration parameter is not used, this method signature will be removed in a future release")]
        public static void AddHystrixUtilizationStream(this IServiceCollection services, IConfiguration config)
            => AddHystrixUtilizationStream(services);

        public static void AddHystrixUtilizationStream(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton(HystrixUtilizationStream.GetInstance());
        }

        [Obsolete("The IConfiguration parameter is not used, this method signature will be removed in a future release")]
        public static void AddHystrixConfigStream(this IServiceCollection services, IConfiguration config)
            => AddHystrixConfigStream(services);

        public static void AddHystrixConfigStream(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton(HystrixConfigurationStream.GetInstance());
        }

        [Obsolete("The IConfiguration parameter is not used, this method signature will be removed in a future release")]
        public static void AddHystrixMonitoringStreams(this IServiceCollection services, IConfiguration config)
            => AddHystrixMonitoringStreams(services);

        public static void AddHystrixMonitoringStreams(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddHystrixMetricsStream();
            services.AddHystrixConfigStream();
            services.AddHystrixRequestEventStream();
            services.AddHystrixUtilizationStream();
        }
    }
}
