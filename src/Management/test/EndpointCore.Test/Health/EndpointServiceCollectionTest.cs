﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Steeltoe.Common.Availability;
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
            IServiceCollection services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();
            IHealthAggregator aggregator = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => EndpointServiceCollectionExtensions.AddHealthActuator(null));
            Assert.Equal("services", ex.ParamName);
            var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddHealthActuator());
            Assert.Equal("config", ex2.ParamName);
            var ex3 = Assert.Throws<ArgumentNullException>(() => services.AddHealthActuator(config, aggregator));
            Assert.Contains(nameof(aggregator), ex3.Message);
        }

        [Fact]
        public void AddHealthActuator_AddsCorrectServicesWithDefaultHealthAggregator()
        {
            var services = new ServiceCollection();
            var appSettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "false",
                ["management:endpoints:path"] = "/cloudfoundryapplication",
                ["management:endpoints:health:enabled"] = "true"
            };
            var configurationBuilder = new ConfigurationBuilder();
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
            var services = new ServiceCollection();
            var appSettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "false",
                ["management:endpoints:path"] = "/cloudfoundryapplication",
                ["management:endpoints:health:enabled"] = "true"
            };
            var configurationBuilder = new ConfigurationBuilder();
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
            Assert.Equal(3, contribsList.Count);
            var availability = serviceProvider.GetService<ApplicationAvailability>();
            Assert.NotNull(availability);
        }

        [Fact]
        public void AddHealthContributors_AddsServices()
        {
            var services = new ServiceCollection();
            EndpointServiceCollectionExtensions.AddHealthContributors(services, typeof(TestContributor));
            var serviceProvider = services.BuildServiceProvider();
            var contribs = serviceProvider.GetServices<IHealthContributor>();
            Assert.NotNull(contribs);
            var contribsList = contribs.ToList();
            Assert.Single(contribsList);
        }
    }
}