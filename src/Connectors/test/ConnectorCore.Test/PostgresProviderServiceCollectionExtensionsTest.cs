// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.CloudFoundry.Connector.Relational;
using Steeltoe.CloudFoundry.Connector.Test;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Collections.Generic;
using System.Data;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.PostgreSql.Test
{
    /// <summary>
    /// Tests for the extension method that adds both the DbConnection and the health check
    /// </summary>
    public class PostgresProviderServiceCollectionExtensionsTest
    {
        public PostgresProviderServiceCollectionExtensionsTest()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
        }

        [Fact]
        public void AddPostgresConnection_ThrowsIfServiceCollectionNull()
        {
            // Arrange
            IServiceCollection services = null;
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => PostgresProviderServiceCollectionExtensions.AddPostgresConnection(services, config));
            Assert.Contains(nameof(services), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => PostgresProviderServiceCollectionExtensions.AddPostgresConnection(services, config, "foobar"));
            Assert.Contains(nameof(services), ex2.Message);
        }

        [Fact]
        public void AddPostgresConnection_ThrowsIfConfigurationNull()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => PostgresProviderServiceCollectionExtensions.AddPostgresConnection(services, config));
            Assert.Contains(nameof(config), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => PostgresProviderServiceCollectionExtensions.AddPostgresConnection(services, config, "foobar"));
            Assert.Contains(nameof(config), ex2.Message);
        }

        [Fact]
        public void AddPostgresConnection_ThrowsIfServiceNameNull()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;
            string serviceName = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => PostgresProviderServiceCollectionExtensions.AddPostgresConnection(services, config, serviceName));
            Assert.Contains(nameof(serviceName), ex.Message);
        }

        [Fact]
        public void AddPostgresConnection_NoVCAPs_AddsPostgresConnection()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act and Assert
            PostgresProviderServiceCollectionExtensions.AddPostgresConnection(services, config);

            var service = services.BuildServiceProvider().GetService<IDbConnection>();
            Assert.NotNull(service);
        }

        [Fact]
        public void AddPostgresConnection_WithServiceName_NoVCAPs_ThrowsConnectorException()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act and Assert
            var ex = Assert.Throws<ConnectorException>(() => PostgresProviderServiceCollectionExtensions.AddPostgresConnection(services, config, "foobar"));
            Assert.Contains("foobar", ex.Message);
        }

        [Fact]
        public void AddPostgresConnection_MultiplePostgresServices_ThrowsConnectorException()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", PostgresTestHelpers.TwoServerVCAP_EDB);

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act and Assert
            var ex = Assert.Throws<ConnectorException>(() => PostgresProviderServiceCollectionExtensions.AddPostgresConnection(services, config));
            Assert.Contains("Multiple", ex.Message);
        }

        [Fact]
        public void AddPostgresConnection_WithVCAPs_AddsPostgresConnection()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", PostgresTestHelpers.SingleServerVCAP_EDB);
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act and Assert
            PostgresProviderServiceCollectionExtensions.AddPostgresConnection(services, config);

            var service = services.BuildServiceProvider().GetService<IDbConnection>();
            Assert.NotNull(service);
            var connString = service.ConnectionString;
            Assert.Contains("1e9e5dae-ed26-43e7-abb4-169b4c3beaff", connString);
            Assert.Contains("5432", connString);
            Assert.Contains("postgres.testcloud.com", connString);
            Assert.Contains("lmu7c96mgl99b2t1hvdgd5q94v", connString);
            Assert.Contains("1e9e5dae-ed26-43e7-abb4-169b4c3beaff", connString);
        }

        [Fact]
        public void AddPostgresConnection_WithAzureVCAPs_AddsPostgresConnection()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", PostgresTestHelpers.SingleServerVCAP_Azure);
            var appsettings = new Dictionary<string, string>()
            {
                ["postgres:client:urlEncodedCredentials"] = "true"
            };

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            builder.AddInMemoryCollection(appsettings);
            var config = builder.Build();

            // Act and Assert
            PostgresProviderServiceCollectionExtensions.AddPostgresConnection(services, config);

            var service = services.BuildServiceProvider().GetService<IDbConnection>();
            Assert.NotNull(service);
            var connString = service.ConnectionString;
            Assert.Contains("Host=2980cfbe-e198-46fd-8f81-966584bb4678.postgres.database.azure.com;", connString);
            Assert.Contains("Port=5432;", connString);
            Assert.Contains("Database=g01w0qnrb7;", connString);
            Assert.Contains("Username=c2cdhwt4nd@2980cfbe-e198-46fd-8f81-966584bb4678;", connString);
            Assert.Contains("Password=Dko4PGJAsQyEj5gj;", connString);
        }

        [Fact]
        public void AddPostgresConnection_WithCruncyVCAPs_AddsPostgresConnection()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", PostgresTestHelpers.SingleServerVCAP_Crunchy);
            var appsettings = new Dictionary<string, string>()
            {
                ["postgres:client:urlEncodedCredentials"] = "true"
            };

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            builder.AddInMemoryCollection(appsettings);
            var config = builder.Build();

            // Act and Assert
            PostgresProviderServiceCollectionExtensions.AddPostgresConnection(services, config);

            var service = services.BuildServiceProvider().GetService<IDbConnection>();
            Assert.NotNull(service);
            var connString = service.ConnectionString;
            Assert.Contains("Host=10.194.45.174;", connString);
            Assert.Contains("Port=5432;", connString);
            Assert.Contains("Database=postgresample;", connString);
            Assert.Contains("Username=steeltoe7b59f5b8a34bce2a3cf873061cfb5815;", connString);
            Assert.Contains("Password=!DQ4Wm!r4omt$h1929!$;", connString);
            Assert.Contains("sslmode=Require;", connString);
            Assert.Contains("pooling=true;", connString);
        }

        [Fact]
        public void AddPosgreSqlConnection_AddsRelationalHealthContributor()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act
            PostgresProviderServiceCollectionExtensions.AddPostgresConnection(services, config);
            var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RelationalHealthContributor;

            // Assert
            Assert.NotNull(healthContributor);
        }

        [Fact]
        public void AddPosgreSqlConnection_DoesntAddRelationalHealthContributor_WhenCommunityHealthCheckExists()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            var cm = new ConnectionStringManager(config);
            var ci = cm.Get<PostgresConnectionInfo>();
            services.AddHealthChecks().AddNpgSql(ci.ConnectionString, name: ci.Name);

            // Act
            PostgresProviderServiceCollectionExtensions.AddPostgresConnection(services, config);
            var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RelationalHealthContributor;

            // Assert
            Assert.Null(healthContributor);
        }

        [Fact]
        public void AddPosgreSqlConnection_AddsRelationalHealthContributor_WhenCommunityHealthCheckExistsAndForced()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            var cm = new ConnectionStringManager(config);
            var ci = cm.Get<PostgresConnectionInfo>();
            services.AddHealthChecks().AddNpgSql(ci.ConnectionString, name: ci.Name);

            // Act
            PostgresProviderServiceCollectionExtensions.AddPostgresConnection(services, config, addSteeltoeHealthChecks: true);
            var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RelationalHealthContributor;

            // Assert
            Assert.NotNull(healthContributor);
        }
    }
}
