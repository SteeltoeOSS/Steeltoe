// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Steeltoe.CircuitBreaker.Hystrix.Config;
using Steeltoe.CircuitBreaker.Hystrix.Metric;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Sample;
using Steeltoe.CircuitBreaker.Hystrix.MetricsStream;
using Steeltoe.CloudFoundry.Connector;
using Steeltoe.CloudFoundry.ConnectorAutofac;
using Steeltoe.Common.Options.Autofac;
using System;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public static class HystrixContainerBuilderExtensions
    {
        private const string HYSTRIX_STREAM_PREFIX = "hystrix:stream";
        private static string[] rabbitAssemblies = new string[] { "RabbitMQ.Client" };
        private static string[] rabbitTypeNames = new string[] { "RabbitMQ.Client.ConnectionFactory" };

        public static void RegisterHystrixMetricsStream(this ContainerBuilder container, IConfiguration config)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            Type rabbitFactory = ConnectorHelpers.FindType(rabbitAssemblies, rabbitTypeNames);
            if (rabbitFactory == null)
            {
                throw new ConnectorException("Unable to find ConnectionFactory, are you missing RabbitMQ assembly");
            }

            container.RegisterInstance(HystrixDashboardStream.GetInstance()).SingleInstance();

            container.RegisterHystrixConnection(config).SingleInstance();

            container.RegisterOption<HystrixMetricsStreamOptions>(config.GetSection(HYSTRIX_STREAM_PREFIX));
            container.RegisterType<RabbitMetricsStreamPublisher>().SingleInstance();
        }

        public static void RegisterHystrixRequestEventStream(this ContainerBuilder container, IConfiguration config)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            container.RegisterInstance(HystrixRequestEventsStream.GetInstance()).SingleInstance();
        }

        public static void RegisterHystrixUtilizationStream(this ContainerBuilder container, IConfiguration config)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            container.RegisterInstance(HystrixUtilizationStream.GetInstance()).SingleInstance();
        }

        public static void RegisterHystrixConfigStream(this ContainerBuilder container, IConfiguration config)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            container.RegisterInstance(HystrixConfigurationStream.GetInstance()).SingleInstance();
        }

        public static void RegisterHystrixMonitoringStreams(this ContainerBuilder container, IConfiguration config)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            container.RegisterHystrixMetricsStream(config);
            container.RegisterHystrixConfigStream(config);
            container.RegisterHystrixRequestEventStream(config);
            container.RegisterHystrixUtilizationStream(config);
        }

        public static void StartHystrixMetricsStream(this IContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            container.Resolve<RabbitMetricsStreamPublisher>();
        }
    }
}
