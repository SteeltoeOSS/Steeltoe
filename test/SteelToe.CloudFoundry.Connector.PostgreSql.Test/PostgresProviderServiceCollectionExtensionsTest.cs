//
// Copyright 2015 the original author or authors.
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
//

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SteelToe.Extensions.Configuration;
using System;
using Xunit;

namespace SteelToe.CloudFoundry.Connector.PostgreSql.Test
{
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
        public void AddPostgresConnection_ThrowsIfConfigurtionNull()
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

           var service = services.BuildServiceProvider().GetService<NpgsqlConnection>();
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
            var env1 = @"
{
      'limits': {
        'fds': 16384,
        'mem': 1024,
        'disk': 1024
      },
      'application_name': 'spring-cloud-broker',
      'application_uris': [
        'spring-cloud-broker.apps.testcloud.com'
      ],
      'name': 'spring-cloud-broker',
      'space_name': 'p-spring-cloud-services',
      'space_id': '65b73473-94cc-4640-b462-7ad52838b4ae',
      'uris': [
        'spring-cloud-broker.apps.testcloud.com'
      ],
      'users': null,
      'version': '07e112f7-2f71-4f5a-8a34-db51dbed30a3',
      'application_version': '07e112f7-2f71-4f5a-8a34-db51dbed30a3',
      'application_id': '798c2495-fe75-49b1-88da-b81197f2bf06'
    }
}";
            var env2 = @"
{
        'EDB-Shared-PostgreSQL': [
            {
                'credentials': {
                    'uri': 'postgres://1e9e5dae-ed26-43e7-abb4-169b4c3beaff:lmu7c96mgl99b2t1hvdgd5q94v@postgres.testcloud.com:5432/1e9e5dae-ed26-43e7-abb4-169b4c3beaff'
                },
            'syslog_drain_url': null,
            'label': 'EDB-Shared-PostgreSQL',
            'provider': null,
            'plan': 'Basic PostgreSQL Plan',
            'name': 'myPostgres',
            'tags': [
                'PostgreSQL',
                'Database storage'
            ]
        },
        {
            'credentials': {
                'uri': 'postgres://1e9e5dae-ed26-43e7-abb4-169b4c3beaff:lmu7c96mgl99b2t1hvdgd5q94v@postgres.testcloud.com:5432/1e9e5dae-ed26-43e7-abb4-169b4c3beaff'
            },
            'syslog_drain_url': null,
            'label': 'EDB-Shared-PostgreSQL',
            'provider': null,
            'plan': 'Basic PostgreSQL Plan',
            'name': 'myPostgres1',
            'tags': [
                'PostgreSQL',
                'Database storage'
            ]
        }
      ]
}
";
            // Arrange
            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", env1);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", env2);

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
            var env1 = @"
{
      'limits': {
        'fds': 16384,
        'mem': 1024,
        'disk': 1024
      },
      'application_name': 'spring-cloud-broker',
      'application_uris': [
        'spring-cloud-broker.apps.testcloud.com'
      ],
      'name': 'spring-cloud-broker',
      'space_name': 'p-spring-cloud-services',
      'space_id': '65b73473-94cc-4640-b462-7ad52838b4ae',
      'uris': [
        'spring-cloud-broker.apps.testcloud.com'
      ],
      'users': null,
      'version': '07e112f7-2f71-4f5a-8a34-db51dbed30a3',
      'application_version': '07e112f7-2f71-4f5a-8a34-db51dbed30a3',
      'application_id': '798c2495-fe75-49b1-88da-b81197f2bf06'
    }
}";
            var env2 = @"
{
        'EDB-Shared-PostgreSQL': [
            {
                'credentials': {
                    'uri': 'postgres://1e9e5dae-ed26-43e7-abb4-169b4c3beaff:lmu7c96mgl99b2t1hvdgd5q94v@postgres.testcloud.com:5432/1e9e5dae-ed26-43e7-abb4-169b4c3beaff'
                },
            'syslog_drain_url': null,
            'label': 'EDB-Shared-PostgreSQL',
            'provider': null,
            'plan': 'Basic PostgreSQL Plan',
            'name': 'myPostgres',
            'tags': [
                'PostgreSQL',
                'Database storage'
            ]
        }
      ]
}
";
            // Arrange
            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", env1);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", env2);

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act and Assert
            PostgresProviderServiceCollectionExtensions.AddPostgresConnection(services, config);

            var service = services.BuildServiceProvider().GetService<NpgsqlConnection>();
            Assert.NotNull(service);
            var connString = service.ConnectionString;
            Assert.True(connString.Contains("1e9e5dae-ed26-43e7-abb4-169b4c3beaff"));
            Assert.True(connString.Contains("5432"));
            Assert.True(connString.Contains("postgres.testcloud.com"));
            Assert.True(connString.Contains("lmu7c96mgl99b2t1hvdgd5q94v"));
            Assert.True(connString.Contains("1e9e5dae-ed26-43e7-abb4-169b4c3beaff"));
        }
    }
}
