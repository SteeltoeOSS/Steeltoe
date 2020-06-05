// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connector.Relational;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using Xunit;

namespace Steeltoe.Connector.SqlServer.Test
{
    /// <summary>
    /// Tests for the extension method that adds just the health check
    /// </summary>
    public class SqlServerServiceCollectionExtensionsTest
    {
        public SqlServerServiceCollectionExtensionsTest()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
        }

        [Fact]
        public void AddSqlServerHealthContributor_ThrowsIfServiceCollectionNull()
        {
            // Arrange
            IServiceCollection services = null;
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => SqlServerServiceCollectionExtensions.AddSqlServerHealthContributor(services, config));
            Assert.Contains(nameof(services), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => SqlServerServiceCollectionExtensions.AddSqlServerHealthContributor(services, config, "foobar"));
            Assert.Contains(nameof(services), ex2.Message);
        }

        [Fact]
        public void AddSqlServerHealthContributor_ThrowsIfConfigurationNull()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => SqlServerServiceCollectionExtensions.AddSqlServerHealthContributor(services, config));
            Assert.Contains(nameof(config), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => SqlServerServiceCollectionExtensions.AddSqlServerHealthContributor(services, config, "foobar"));
            Assert.Contains(nameof(config), ex2.Message);
        }

        [Fact]
        public void AddSqlServerHealthContributor_ThrowsIfServiceNameNull()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;
            string serviceName = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => SqlServerServiceCollectionExtensions.AddSqlServerHealthContributor(services, config, serviceName));
            Assert.Contains(nameof(serviceName), ex.Message);
        }

        [Fact]
        public void AddSqlServerHealthContributor_NoVCAPs_AddsIHealthContributor()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();

            // Act and Assert
            SqlServerServiceCollectionExtensions.AddSqlServerHealthContributor(services, config);

            var service = services.BuildServiceProvider().GetService<IHealthContributor>();
            Assert.NotNull(service);
        }

        [Fact]
        public void AddSqlServerHealthContributor_WithServiceName_NoVCAPs_ThrowsConnectorException()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();

            // Act and Assert
            var ex = Assert.Throws<ConnectorException>(() => SqlServerServiceCollectionExtensions.AddSqlServerHealthContributor(services, config, "foobar"));
            Assert.Contains("foobar", ex.Message);
        }

        [Fact]
        public void AddSqlServerHealthContributor_AddsRelationalHealthContributor()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            var builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act
            SqlServerServiceCollectionExtensions.AddSqlServerHealthContributor(services, config);
            var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RelationalHealthContributor;

            // Assert
            Assert.NotNull(healthContributor);
        }
    }
}
