//
// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
            var publisher = provider.GetService<HystrixMetricsStreamPublisher>();
            Assert.NotNull(publisher);
            var factory = provider.GetService<HystrixConnectionFactory>();
            Assert.NotNull(factory);

            publisher.sampleSubscription.Dispose();

        }
    }
}
