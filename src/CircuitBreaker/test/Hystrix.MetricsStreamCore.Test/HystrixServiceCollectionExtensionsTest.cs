// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CloudFoundry.Connector.Hystrix;
using System;

using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsStream.Test
{
    public class HystrixServiceCollectionExtensionsTest : HystrixTestBase
    {
        [Fact]
        public void AddHystrixStreams_ThrowsIfServiceContainerNull()
        {
            IServiceCollection services = null;
            IConfiguration config = new ConfigurationBuilder().Build();

            var ex = Assert.Throws<ArgumentNullException>(() => services.AddHystrixConfigStream(config));
            Assert.Contains(nameof(services), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixMetricsStream(config));
            Assert.Contains(nameof(services), ex2.Message);

            var ex3 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixMonitoringStreams(config));
            Assert.Contains(nameof(services), ex3.Message);

            var ex4 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixRequestEventStream(config));
            Assert.Contains(nameof(services), ex4.Message);

            var ex5 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixMonitoringStreams(config));
            Assert.Contains(nameof(services), ex5.Message);
        }

        [Fact]
        public void AddHystrixMetricsStream_AddsExpectedServices()
        {
            IServiceCollection services = new ServiceCollection();
            IConfiguration config = new ConfigurationBuilder().Build();
            services.AddOptions();
            services.AddHystrixMetricsStream(config);
            var provider = services.BuildServiceProvider();

            var dashStream = provider.GetService<HystrixDashboardStream>();
            Assert.NotNull(dashStream);
            var options = provider.GetService<IOptions<HystrixMetricsStreamOptions>>();
            Assert.NotNull(options);
            var publisher = provider.GetService<RabbitMetricsStreamPublisher>();
            Assert.NotNull(publisher);
            var factory = provider.GetService<HystrixConnectionFactory>();
            Assert.NotNull(factory);

            publisher.SampleSubscription.Dispose();
        }
    }
}
