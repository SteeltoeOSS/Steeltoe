// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.CloudFoundry.Connector.Relational;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using Xunit;

namespace Steeltoe.CloudFoundry.ConnectorCore.Oracle.Test
{
    public class OracleServiceCollectionExtensionsTest
    {
        [Fact]
        public void AddOracleHealthContributor_ThrowsIfServiceCollectionNull()
        {
            // Arrange
            IServiceCollection services = null;
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => OracleServiceCollectionExtensions.AddOracleHealthContributor(services, config));
            Assert.Contains(nameof(services), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => OracleServiceCollectionExtensions.AddOracleHealthContributor(services, config, "foobar"));
            Assert.Contains(nameof(services), ex2.Message);
        }

        [Fact]
        public void AddOracleHealthContributor_ThrowsIfConfigurationNull()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => OracleServiceCollectionExtensions.AddOracleHealthContributor(services, config));
            Assert.Contains(nameof(config), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => OracleServiceCollectionExtensions.AddOracleHealthContributor(services, config, "foobar"));
            Assert.Contains(nameof(config), ex2.Message);
        }

        [Fact]
        public void AddOracleHealthContributor_ThrowsIfServiceNameNull()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;
            string serviceName = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => OracleServiceCollectionExtensions.AddOracleHealthContributor(services, config, serviceName));
            Assert.Contains(nameof(serviceName), ex.Message);
        }

        [Fact]
        public void AddOracleHealthContributor_AddsRelationalHealthContributor()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act
            OracleServiceCollectionExtensions.AddOracleHealthContributor(services, config);
            var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RelationalHealthContributor;

            // Assert
            Assert.NotNull(healthContributor);
        }
    }
}
