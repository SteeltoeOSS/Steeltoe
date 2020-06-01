// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Health.Contributor;
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
        public void AddHealthActuator_AddsCorrectServicesWithDefaultHealthAggregator()
        {
            ServiceCollection services = new ServiceCollection();
            var appSettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "false",
                ["management:endpoints:path"] = "/cloudfoundryapplication",
                ["management:endpoints:health:enabled"] = "true"
            };
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appSettings);
            var config = configurationBuilder.Build();

            services.AddHealthActuator(config, new DefaultHealthAggregator(), typeof(DiskSpaceContributor));

            services.Configure<HealthCheckServiceOptions>(config);
            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetService<IHealthOptions>();
            Assert.NotNull(options);
            var ep = serviceProvider.GetService<HealthEndpointCore>();
            Assert.NotNull(ep);
            var agg = serviceProvider.GetService<IHealthAggregator>();
            Assert.NotNull(agg);
            var contribs = serviceProvider.GetServices<IHealthContributor>();
            Assert.NotNull(contribs);
            var contribsList = contribs.ToList();
            Assert.Single(contribsList);
        }

        [Fact]
        public void AddHealthActuator_AddsCorrectServices()
        {
            ServiceCollection services = new ServiceCollection();
            var appSettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "false",
                ["management:endpoints:path"] = "/cloudfoundryapplication",
                ["management:endpoints:health:enabled"] = "true"
            };
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appSettings);
            var config = configurationBuilder.Build();

            services.AddHealthActuator(config);

            services.Configure<HealthCheckServiceOptions>(config);
            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetService<IHealthOptions>();
            Assert.NotNull(options);
            var ep = serviceProvider.GetService<HealthEndpointCore>();
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
            EndpointServiceCollectionExtensions.AddHealthContributors(services, typeof(TestContributor));
            var serviceProvider = services.BuildServiceProvider();
            var contribs = serviceProvider.GetServices<IHealthContributor>();
            Assert.NotNull(contribs);
            var contribsList = contribs.ToList();
            Assert.Single(contribsList);
        }

        private int IOptionsMonitor<T>()
        {
            throw new NotImplementedException();
        }
    }
}