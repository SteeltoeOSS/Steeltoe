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

            var ex5 = Assert.Throws<ArgumentNullException>(() => services.AddHystrixMonitoringStreams(config));
            Assert.Contains(nameof(services), ex5.Message);
        }
    }
}
