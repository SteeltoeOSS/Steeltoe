// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.CircuitBreaker.Hystrix.Config;
using Steeltoe.CircuitBreaker.Hystrix.Metric;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Sample;
using Steeltoe.CircuitBreaker.Hystrix.MetricsStream;
using Steeltoe.Connector.Hystrix;
using System;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public static class HystrixServiceCollectionExtensions
    {
        private const string HYSTRIX_STREAM_PREFIX = "hystrix:stream";

        public static void AddHystrixMetricsStream(this IServiceCollection services, IConfiguration config)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<HystrixDashboardStream>(HystrixDashboardStream.GetInstance());
            services.AddHystrixConnection(config);
            services.Configure<HystrixMetricsStreamOptions>(config.GetSection(HYSTRIX_STREAM_PREFIX));
            services.AddSingleton<RabbitMetricsStreamPublisher>();
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
