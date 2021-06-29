// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsEvents.Test
{
    public class HystrixServiceCollectionExtensionsTest
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
        }

        [Fact]
        public void AddHystrixMetricsEventSource_ThrowsIfServiceContainerNull()
        {
            IServiceCollection services = null;
            var ex5 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixMetricsEventSource());
            Assert.Contains(nameof(services), ex5.Message);
        }

        [Fact]
        public void AddHystrixMetricsEventSource_AddsHostedService()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddHystrixMetricsEventSource();
        }
    }
}
