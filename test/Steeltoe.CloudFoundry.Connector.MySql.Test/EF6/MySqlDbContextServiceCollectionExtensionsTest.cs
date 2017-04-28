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
#if NET452

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.Entity;
using Steeltoe.Extensions.Configuration;
using System;
using System.Data.Entity;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.MySql.EF6.Test
{
    public class MySqlDbContextServiceCollectionExtensionsTest
    {
        public MySqlDbContextServiceCollectionExtensionsTest()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
        }

        [Fact]
        public void AddDbContext_ThrowsIfServiceCollectionNull()
        {
            // Arrange
            IServiceCollection services = null;
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => MySqlDbContextServiceCollectionExtensions.AddDbContext<GoodDbContext>(services, config));
            Assert.Contains(nameof(services), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => MySqlDbContextServiceCollectionExtensions.AddDbContext<GoodDbContext>(services, config, "foobar"));
            Assert.Contains(nameof(services), ex2.Message);

        }
        [Fact]
        public void AddDbContext_ThrowsIfConfigurtionNull()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => MySqlDbContextServiceCollectionExtensions.AddDbContext<GoodDbContext>(services, config));
            Assert.Contains(nameof(config), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => MySqlDbContextServiceCollectionExtensions.AddDbContext<GoodDbContext>(services, config, "foobar"));
            Assert.Contains(nameof(config), ex2.Message);

        }

        [Fact]
        public void AddDbContext_ThrowsIfServiceNameNull()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;
            string serviceName = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => MySqlDbContextServiceCollectionExtensions.AddDbContext<GoodDbContext>(services, config, serviceName));
            Assert.Contains(nameof(serviceName), ex.Message);

        }

        [Fact]
        public void AddDbContext_NoVCAPs_AddsDbContext()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act and Assert
            MySqlDbContextServiceCollectionExtensions.AddDbContext<GoodDbContext>(services, config);

            var service = services.BuildServiceProvider().GetService<GoodDbContext>();
            Assert.NotNull(service);

        }

        [Fact]
        public void AddDbContext_WithServiceName_NoVCAPs_ThrowsConnectorException()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act and Assert
            var ex = Assert.Throws<ConnectorException>(() => MySqlDbContextServiceCollectionExtensions.AddDbContext<GoodDbContext>(services, config, "foobar"));
            Assert.Contains("foobar", ex.Message);

        }

        [Fact]
        public void AddDbContext_MultipleMySqlServices_ThrowsConnectorException()
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
      'p-msql': [
        {
          'credentials': {
            'hostname': '192.168.0.90',
            'port': 3306,
            'name': 'cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355',
            'username': 'Dd6O1BPXUHdrmzbP',
            'password': '7E1LxXnlH2hhlPVt',
            'uri': 'mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true',
            'jdbcUrl': 'jdbc:mysql://192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt'
          },
          'syslog_drain_url': null,
          'label': 'p-mysql',
          'provider': null,
          'plan': '100mb-dev',
          'name': 'spring-cloud-broker-db',
          'tags': [
            'mysql',
            'relational'
          ]
        },
        {
          'credentials': {
            'hostname': '192.168.0.90',
            'port': 3306,
            'name': 'cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355',
            'username': 'Dd6O1BPXUHdrmzbP',
            'password': '7E1LxXnlH2hhlPVt',
            'uri': 'mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true',
            'jdbcUrl': 'jdbc:mysql://192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt'
          },
          'syslog_drain_url': null,
          'label': 'p-mysql',
          'provider': null,
          'plan': '100mb-dev',
          'name': 'spring-cloud-broker-db2',
          'tags': [
            'mysql',
            'relational'
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
            var ex = Assert.Throws<ConnectorException>(() => MySqlDbContextServiceCollectionExtensions.AddDbContext<GoodDbContext>(services, config));
            Assert.Contains("Multiple", ex.Message);

        }
        [Fact]
        public void AddDbContexts_WithVCAPs_AddsDbContexts()
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
      'p-msql': [
        {
          'credentials': {
            'hostname': '192.168.0.90',
            'port': 3306,
            'name': 'cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355',
            'username': 'Dd6O1BPXUHdrmzbP',
            'password': '7E1LxXnlH2hhlPVt',
            'uri': 'mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true',
            'jdbcUrl': 'jdbc:mysql://192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt'
          },
          'syslog_drain_url': null,
          'label': 'p-mysql',
          'provider': null,
          'plan': '100mb-dev',
          'name': 'spring-cloud-broker-db',
          'tags': [
            'mysql',
            'relational'
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
            MySqlDbContextServiceCollectionExtensions.AddDbContext<GoodDbContext>(services, config);
            MySqlDbContextServiceCollectionExtensions.AddDbContext<Good2DbContext>(services, config);

            var built = services.BuildServiceProvider();
            var service = built.GetService<GoodDbContext>();
            Assert.NotNull(service);

            var service2 = built.GetService<Good2DbContext>();
            Assert.NotNull(service2);

        }
    }

    [DbConfigurationType(typeof(MySqlEFConfiguration))]
    class Good2DbContext : DbContext
    {
        public Good2DbContext(string str)
        {

        }
    }
}
#endif
