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
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Steeltoe.Management.Endpoint.Health.Test
{
    public class EndpointServiceCollectionTest : BaseTest
    {
        [Fact]
        public void AddHealthActuator_ThrowsOnNulls()
        {
            // Arrange
            IServiceCollection services = null;
            IServiceCollection services2 = new ServiceCollection();
            IConfigurationRoot config = null;
            IConfigurationRoot config2 = new ConfigurationBuilder().Build();
            IHealthAggregator aggregator = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => EndpointServiceCollectionExtensions.AddHealthActuator(services, config));
            Assert.Contains(nameof(services), ex.Message);
            var ex2 = Assert.Throws<ArgumentNullException>(() => EndpointServiceCollectionExtensions.AddHealthActuator(services2, config));
            Assert.Contains(nameof(config), ex2.Message);
            var ex3 = Assert.Throws<ArgumentNullException>(() => EndpointServiceCollectionExtensions.AddHealthActuator(services2, config2, aggregator));
            Assert.Contains(nameof(aggregator), ex3.Message);
        }

        [Fact]
        public void AddHealthActuator_AddsCorrectServices()
        {
            ServiceCollection services = new ServiceCollection();
            var appSettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "false",
                ["management:endpoints:sensitive"] = "false",
                ["management:endpoints:path"] = "/cloudfoundryapplication",
                ["management:endpoints:health:enabled"] = "true",
                ["management:endpoints:health:sensitive"] = "false"
            };
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appSettings);
            var config = configurationBuilder.Build();

            services.AddHealthActuator(config);

            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetService<IHealthOptions>();
            Assert.NotNull(options);
            var ep = serviceProvider.GetService<HealthEndpoint>();
            Assert.NotNull(ep);
            var agg = serviceProvider.GetService<IHealthAggregator>();
            Assert.NotNull(agg);
            var contribs = serviceProvider.GetServices<IHealthContributor>();
            Assert.NotNull(contribs);
            var contribsList = contribs.ToList();
            Assert.Single(contribsList);
        }

        [Fact]
        public void AddHealthContributors_AddsServices()
        {
            ServiceCollection services = new ServiceCollection();
            EndpointServiceCollectionExtensions.AddHealthContributors(services, new TestContributor());
            var serviceProvider = services.BuildServiceProvider();
            var contribs = serviceProvider.GetServices<IHealthContributor>();
            Assert.NotNull(contribs);
            var contribsList = contribs.ToList();
            Assert.Single(contribsList);
        }
    }
}