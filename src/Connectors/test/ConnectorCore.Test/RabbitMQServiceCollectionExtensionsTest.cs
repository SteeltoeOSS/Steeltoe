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
using RabbitMQ.Client;
using Steeltoe.CloudFoundry.Connector.Test;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.RabbitMQ.Test
{
    public class RabbitMQServiceCollectionExtensionsTest
    {
        public RabbitMQServiceCollectionExtensionsTest()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
        }

        [Fact]
        public void AddRabbitMQConnection_ThrowsIfServiceCollectionNull()
        {
            // Arrange
            IServiceCollection services = null;
            IConfigurationRoot config = null;

            // Act and Assert
            var ex =
                Assert.Throws<ArgumentNullException>(
                    () => RabbitMQProviderServiceCollectionExtensions.AddRabbitMQConnection(services, config));
            Assert.Contains(nameof(services), ex.Message);

            var ex2 =
                Assert.Throws<ArgumentNullException>(
                    () => RabbitMQProviderServiceCollectionExtensions.AddRabbitMQConnection(services, config, "foobar"));
            Assert.Contains(nameof(services), ex2.Message);
        }

        [Fact]
        public void AddRabbitMQConnection_ThrowsIfConfigurationNull()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;

            // Act and Assert
            var ex =
                Assert.Throws<ArgumentNullException>(
                    () => RabbitMQProviderServiceCollectionExtensions.AddRabbitMQConnection(services, config));
            Assert.Contains(nameof(config), ex.Message);

            var ex2 =
                Assert.Throws<ArgumentNullException>(
                    () => RabbitMQProviderServiceCollectionExtensions.AddRabbitMQConnection(services, config, "foobar"));
            Assert.Contains(nameof(config), ex2.Message);
        }

        [Fact]
        public void AddRabbitMQConnection_ThrowsIfServiceNameNull()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;
            string serviceName = null;

            // Act and Assert
            var ex =
                Assert.Throws<ArgumentNullException>(
                    () => RabbitMQProviderServiceCollectionExtensions.AddRabbitMQConnection(services, config, serviceName));
            Assert.Contains(nameof(serviceName), ex.Message);
        }

        [Fact]
        public void AddRabbitMQConnection_NoVCAPs_AddsConfiguredConnection()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act and Assert
            RabbitMQProviderServiceCollectionExtensions.AddRabbitMQConnection(services, config);

            var service = services.BuildServiceProvider().GetService<IConnectionFactory>();
            Assert.NotNull(service);
        }

        [Fact]
        public void AddRabbitMQConnection_WithServiceName_NoVCAPs_ThrowsConnectorException()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act and Assert
            var ex =
                Assert.Throws<ConnectorException>(
                    () => RabbitMQProviderServiceCollectionExtensions.AddRabbitMQConnection(services, config, "foobar"));
            Assert.Contains("foobar", ex.Message);
        }

        [Fact]
        public void AddRabbitMQConnection_MultipleRabbitMQServices_ThrowsConnectorException()
        {
            // Arrange
            var env2 = @"
{
      'p-rabbitmq': [
        {
            'credentials': {
                'uri': 'amqp://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355'
            },
          'syslog_drain_url': null,
          'label': 'p-rabbitmq',
          'provider': null,
          'plan': 'standard',
          'name': 'myRabbitMQService1',
          'tags': [
            'rabbitmq',
            'amqp'
          ]
        }, 
        {
            'credentials': {
                'uri': 'amqp://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355'
            },
          'syslog_drain_url': null,
          'label': 'p-Rabbit',
          'provider': null,
          'plan': 'standard',
          'name': 'myRabbitMQService2',
          'tags': [
            'rabbitmq',
            'amqp'
          ]
        } 
      ]
}
";

            // Arrange
            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", env2);

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act and Assert
            var ex =
                Assert.Throws<ConnectorException>(
                    () => RabbitMQProviderServiceCollectionExtensions.AddRabbitMQConnection(services, config));
            Assert.Contains("Multiple", ex.Message);
        }

        [Fact]
        public void AddRabbitMQConnection_MultipleRabbitMQServices_DoesntThrow_IfNameUsed()
        {
            // Arrange
            var env2 = @"
{
      'p-rabbitmq': [
        {
            'credentials': {
                'uri': 'amqp://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355'
            },
          'syslog_drain_url': null,
          'label': 'p-rabbitmq',
          'provider': null,
          'plan': 'standard',
          'name': 'myRabbitMQService1',
          'tags': [
            'rabbitmq',
            'amqp'
          ]
        }, 
        {
            'credentials': {
                'uri': 'amqp://a:b@192.168.0.91:3306/asdf'
            },
          'syslog_drain_url': null,
          'label': 'p-Rabbit',
          'provider': null,
          'plan': 'standard',
          'name': 'myRabbitMQService2',
          'tags': [
            'rabbitmq',
            'amqp'
          ]
        } 
      ]
}
";

            // Arrange
            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", env2);

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act
            RabbitMQProviderServiceCollectionExtensions.AddRabbitMQConnection(services, config, "myRabbitMQService2");
            var service = services.BuildServiceProvider().GetService<IConnectionFactory>() as ConnectionFactory;
            Assert.NotNull(service);
            Assert.Equal("asdf", service.VirtualHost);
            Assert.Equal(3306, service.Port);
            Assert.Equal("192.168.0.91", service.HostName);
            Assert.Equal("a", service.UserName);
            Assert.Equal("b", service.Password);
        }

        [Fact]
        public void AddRabbitMQConnection_WithVCAPs_AddsRabbitMQConnection()
        {
            // Arrange
            var env2 = @"
{
      'p-rabbitmq': [
        {
          'credentials': {
            'uri': 'amqp://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355'
          },
          'syslog_drain_url': null,
          'label': 'p-rabbitmq',
          'provider': null,
          'plan': 'standard',
          'name': 'myRabbitMQService',
          'tags': [
            'rabbitmq',
            'amqp'
          ]
        }
      ]
}
";

            // Arrange
            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", env2);

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act and Assert
            RabbitMQProviderServiceCollectionExtensions.AddRabbitMQConnection(services, config);

            var service = services.BuildServiceProvider().GetService<IConnectionFactory>() as ConnectionFactory;
            Assert.NotNull(service);
            Assert.Equal("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", service.VirtualHost);
            Assert.Equal(3306, service.Port);
            Assert.Equal("192.168.0.90", service.HostName);
            Assert.Equal("Dd6O1BPXUHdrmzbP", service.UserName);
            Assert.Equal("7E1LxXnlH2hhlPVt", service.Password);
        }

        [Fact]
        public void AddRabbitMQConnection_AddsRabbitMQHealthContributor()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act
            RabbitMQProviderServiceCollectionExtensions.AddRabbitMQConnection(services, config);
            var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RabbitMQHealthContributor;

            // Assert
            Assert.NotNull(healthContributor);
        }

        [Fact]
        public void AddRabbitMQConnection_DoesntAddsRabbitMQHealthContributor_WhenCommunityHealthCheckExists()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            var cm = new ConnectionStringManager(config);
            var ci = cm.Get<RabbitMQConnectionInfo>();
            services.AddHealthChecks().AddRabbitMQ(ci.ConnectionString, name: ci.Name);

            // Act
            RabbitMQProviderServiceCollectionExtensions.AddRabbitMQConnection(services, config);
            var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RabbitMQHealthContributor;

            // Assert
            Assert.Null(healthContributor);
        }

        [Fact]
        public void AddRabbitMQConnection_AddsRabbitMQHealthContributor_WhenCommunityHealthCheckExistsAndForced()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            var cm = new ConnectionStringManager(config);
            var ci = cm.Get<RabbitMQConnectionInfo>();
            services.AddHealthChecks().AddRabbitMQ(ci.ConnectionString, name: ci.Name);

            // Act
            RabbitMQProviderServiceCollectionExtensions.AddRabbitMQConnection(services, config, addSteeltoeHealthChecks: true);
            var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RabbitMQHealthContributor;

            // Assert
            Assert.NotNull(healthContributor);
        }
    }
}
