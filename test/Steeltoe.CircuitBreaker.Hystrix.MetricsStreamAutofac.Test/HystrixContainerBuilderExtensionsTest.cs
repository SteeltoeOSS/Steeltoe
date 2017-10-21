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

using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CloudFoundry.Connector.Hystrix;
using Steeltoe.Common.Options.Autofac;
using System;

using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsStream.Test
{
    public class HystrixContainerBuilderExtensionsTest : HystrixTestBase
    {

        [Fact]
        public void RegisterHystrixStreams_ThrowsIfBuilderNull()
        {
            ContainerBuilder container = null;
            IConfiguration config = new ConfigurationBuilder().Build();

            var ex = Assert.Throws<ArgumentNullException>(() => container.RegisterHystrixConfigStream(config));
            Assert.Contains(nameof(container), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => container.RegisterHystrixMetricsStream(config));
            Assert.Contains(nameof(container), ex2.Message);

            var ex3 = Assert.Throws<ArgumentNullException>(() => container.RegisterHystrixMonitoringStreams(config));
            Assert.Contains(nameof(container), ex3.Message);

            var ex4 = Assert.Throws<ArgumentNullException>(() => container.RegisterHystrixRequestEventStream(config));
            Assert.Contains(nameof(container), ex4.Message);

            var ex5 = Assert.Throws<ArgumentNullException>(() => container.RegisterHystrixMonitoringStreams(config));
            Assert.Contains(nameof(container), ex5.Message);
        }

        [Fact]
        public void RegisterHystrixMetricsStream_AddsExpectedServices()
        {
            ContainerBuilder services = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();
            services.RegisterOptions();
            services.RegisterHystrixMetricsStream(config);
            var provider = services.Build();

            var dashStream = provider.Resolve<HystrixDashboardStream>();
            Assert.NotNull(dashStream);
            var options = provider.Resolve<IOptions<HystrixMetricsStreamOptions>>();
            Assert.NotNull(options);
            var publisher = provider.Resolve<RabbitMetricsStreamPublisher>();
            Assert.NotNull(publisher);
            var factory = provider.Resolve<HystrixConnectionFactory>();
            Assert.NotNull(factory);

            publisher.sampleSubscription.Dispose();

        }

        [Fact]
        public void StartHystrixMetricsStream_ThrowsIfContainerNull()
        {
            IContainer container = null;

            var ex = Assert.Throws<ArgumentNullException>(() => container.StartHystrixMetricsStream());
            Assert.Contains(nameof(container), ex.Message);
        }
    }
}
