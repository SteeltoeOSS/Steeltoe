// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Steeltoe.CloudFoundry.Connector.Test;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.MongoDb.Test
{
    public class MongoDbProviderServiceCollectionExtensionsTest
    {
        public MongoDbProviderServiceCollectionExtensionsTest()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
        }

        [Fact]
        public void AddMongoClient_ThrowsIfServiceCollectionNull()
        {
            // Arrange
            IServiceCollection services = null;
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => services.AddMongoClient(config));
            Assert.Contains(nameof(services), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddMongoClient(config, "foobar"));
            Assert.Contains(nameof(services), ex2.Message);
        }

        [Fact]
        public void AddMongoClient_ThrowsIfConfigurationNull()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => services.AddMongoClient(config));
            Assert.Contains(nameof(config), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddMongoClient(config, "foobar"));
            Assert.Contains(nameof(config), ex2.Message);
        }

        [Fact]
        public void AddMongoClient_ThrowsIfServiceNameNull()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;
            string serviceName = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => services.AddMongoClient(config, serviceName));
            Assert.Contains(nameof(serviceName), ex.Message);
        }

        [Fact]
        public void AddMongoClient_NoVCAPs_AddsMongoClient()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act
            services.AddMongoClient(config);
            var service = services.BuildServiceProvider().GetService<MongoClient>();

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void AddMongoClient_WithServiceName_NoVCAPs_ThrowsConnectorException()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act and Assert
            var ex = Assert.Throws<ConnectorException>(() => services.AddMongoClient(config, "foobar"));
            Assert.Contains("foobar", ex.Message);
        }

        [Fact]
        public void AddMongoClient_MultipleMongoDbServices_ThrowsConnectorException()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", MongoDbTestHelpers.DoubleBinding_Enterprise_VCAP);

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act and Assert
            var ex = Assert.Throws<ConnectorException>(() => services.AddMongoClient(config));
            Assert.Contains("Multiple", ex.Message);
        }

        [Fact]
        public void AddMongoClient_With_Enterprise_VCAPs_AddsMongoClient()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", MongoDbTestHelpers.SingleBinding_Enterprise_VCAP);
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act
            services.AddMongoClient(config);
            var service = services.BuildServiceProvider().GetService<MongoClient>();
            var serviceByInterface = services.BuildServiceProvider().GetService<IMongoClient>();

            // Assert
            Assert.NotNull(service);
            Assert.NotNull(serviceByInterface);
            var connSettings = service.Settings;
            Assert.Equal(28000, connSettings.Server.Port);
            Assert.Equal("192.168.12.22", connSettings.Server.Host);
            Assert.Equal("pcf_b8ce63777ce39d1c7f871f2585ba9474", connSettings.Credential.Username);
        }

        [Fact]
        public void AddMongoClient_With_a9s_single_VCAPs_AddsMongoClient()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", MongoDbTestHelpers.SingleBinding_a9s_SingleServer_VCAP);
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act
            services.AddMongoClient(config);
            var service = services.BuildServiceProvider().GetService<MongoClient>();

            // Assert
            Assert.NotNull(service);
            var connSettings = service.Settings;
            Assert.Single(connSettings.Servers);
            Assert.Equal("d8790b7-mongodb-0.node.dc1.a9s-mongodb-consul", connSettings.Server.Host);
            Assert.Equal(27017, connSettings.Server.Port);
            Assert.Equal("a9s-brk-usr-377ad48194cbf0452338737d7f6aa3fb6cdabc24", connSettings.Credential.Username);
        }

        [Fact]
        public void AddMongoClient_With_a9s_replicas_VCAPs_AddsMongoClient()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", MongoDbTestHelpers.SingleBinding_a9s_WithReplicas_VCAP);
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act
            services.AddMongoClient(config);
            var service = services.BuildServiceProvider().GetService<MongoClient>();

            // Assert
            Assert.NotNull(service);
            var connSettings = service.Settings;
            Assert.Contains(new MongoServerAddress("d5584e9-mongodb-0.node.dc1.a9s-mongodb-consul", 27017), connSettings.Servers);
            Assert.Contains(new MongoServerAddress("d5584e9-mongodb-1.node.dc1.a9s-mongodb-consul", 27017), connSettings.Servers);
            Assert.Contains(new MongoServerAddress("d5584e9-mongodb-2.node.dc1.a9s-mongodb-consul", 27017), connSettings.Servers);
            Assert.Equal("a9s-brk-usr-e74b9538ae5dcf04500eb0fc18907338d4610f30", connSettings.Credential.Username);
        }

        [Fact]
        public void AddMongoClientConnection_AddsMongoDbHealthContributor()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act
            services.AddMongoClient(config);
            var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as MongoDbHealthContributor;

            // Assert
            Assert.NotNull(healthContributor);
        }

        [Fact]
        public void AddMongoClientConnection_AddingCommunityContributor_DoesntAddSteeltoeHealthCheck()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", MongoDbTestHelpers.SingleBinding_Enterprise_VCAP);

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            var cm = new ConnectionStringManager(config);
            var ci = cm.Get<MongoDbConnectionInfo>();
            services.AddHealthChecks().AddMongoDb(ci.ConnectionString, name: ci.Name);

            // Act
            services.AddMongoClient(config, "steeltoe");
            var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as MongoDbHealthContributor;

            // Assert
            Assert.Null(healthContributor);
        }

        [Fact]
        public void AddMongoClientConnection_AddingCommunityContributor_AddsSteeltoeHealthCheckWhenForced()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", MongoDbTestHelpers.SingleBinding_Enterprise_VCAP);

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            var cm = new ConnectionStringManager(config);
            var ci = cm.Get<MongoDbConnectionInfo>();
            services.AddHealthChecks().AddMongoDb(ci.ConnectionString, name: ci.Name);

            // Act
            services.AddMongoClient(config, "steeltoe", addSteeltoeHealthChecks: true);
            var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as MongoDbHealthContributor;

            // Assert
            Assert.NotNull(healthContributor);
        }
    }
}
