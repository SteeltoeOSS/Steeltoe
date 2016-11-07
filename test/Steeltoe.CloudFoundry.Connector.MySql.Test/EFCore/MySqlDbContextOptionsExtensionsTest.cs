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
#if !NET451
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using Steeltoe.Extensions.Configuration;
using System;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.MySql.EFCore.Test
{
    public class MySqlDbContextOptionsExtensionsTest
    {
        public MySqlDbContextOptionsExtensionsTest()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
        }

        [Fact]
        public void UseMySql_ThrowsIfDbContextOptionsBuilderNull()
        {
            // Arrange
            DbContextOptionsBuilder optionsBuilder = null;
            DbContextOptionsBuilder<GoodDbContext> goodBuilder = null;
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => MySqlDbContextOptionsExtensions.UseMySql(optionsBuilder, config));
            Assert.Contains(nameof(optionsBuilder), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => MySqlDbContextOptionsExtensions.UseMySql(optionsBuilder, config, "foobar"));
            Assert.Contains(nameof(optionsBuilder), ex2.Message);

            var ex3 = Assert.Throws<ArgumentNullException>(() => MySqlDbContextOptionsExtensions.UseMySql<GoodDbContext>(goodBuilder, config));
            Assert.Contains(nameof(optionsBuilder), ex3.Message);

            var ex4 = Assert.Throws<ArgumentNullException>(() => MySqlDbContextOptionsExtensions.UseMySql<GoodDbContext>(goodBuilder, config, "foobar"));
            Assert.Contains(nameof(optionsBuilder), ex4.Message);

        }
        [Fact]
        public void UseMySql_ThrowsIfConfigurtionNull()
        {
            // Arrange
            DbContextOptionsBuilder optionsBuilder = new DbContextOptionsBuilder();
            DbContextOptionsBuilder<GoodDbContext> goodBuilder = new DbContextOptionsBuilder<GoodDbContext>();
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => MySqlDbContextOptionsExtensions.UseMySql(optionsBuilder, config));
            Assert.Contains(nameof(config), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => MySqlDbContextOptionsExtensions.UseMySql(optionsBuilder, config, "foobar"));
            Assert.Contains(nameof(config), ex2.Message);

            var ex3 = Assert.Throws<ArgumentNullException>(() => MySqlDbContextOptionsExtensions.UseMySql<GoodDbContext>(goodBuilder, config));
            Assert.Contains(nameof(config), ex3.Message);

            var ex4 = Assert.Throws<ArgumentNullException>(() => MySqlDbContextOptionsExtensions.UseMySql<GoodDbContext>(goodBuilder, config, "foobar"));
            Assert.Contains(nameof(config), ex4.Message);

        }

        [Fact]
        public void UseMySql_ThrowsIfServiceNameNull()
        {
            // Arrange
            DbContextOptionsBuilder optionsBuilder = new DbContextOptionsBuilder();
            DbContextOptionsBuilder<GoodDbContext> goodBuilder = new DbContextOptionsBuilder<GoodDbContext>();
            IConfigurationRoot config = new ConfigurationBuilder().Build();
            string serviceName = null;

            // Act and Assert
            var ex2 = Assert.Throws<ArgumentException>(() => MySqlDbContextOptionsExtensions.UseMySql(optionsBuilder, config, serviceName));
            Assert.Contains(nameof(serviceName), ex2.Message);

            var ex4 = Assert.Throws<ArgumentException>(() => MySqlDbContextOptionsExtensions.UseMySql<GoodDbContext>(goodBuilder, config, serviceName));
            Assert.Contains(nameof(serviceName), ex4.Message);

        }
        [Fact]
        public void AddDbContext_NoVCAPs_AddsDbContext_WithPostgresConnection()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act and Assert
            services.AddDbContext<GoodDbContext>(options =>
                    options.UseMySql(config));

            var service = services.BuildServiceProvider().GetService<GoodDbContext>();
            Assert.NotNull(service);
            var con = service.Database.GetDbConnection();
            Assert.NotNull(con);
            Assert.NotNull(con as MySqlConnection);

        }

        [Fact]
        public void AddDbContext_WithServiceName_NoVCAPs_ThrowsConnectorException()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act and Assert
            services.AddDbContext<GoodDbContext>(options =>
                  options.UseMySql(config, "foobar"));

            var ex = Assert.Throws<ConnectorException>(() => services.BuildServiceProvider().GetService<GoodDbContext>());
            Assert.Contains("foobar", ex.Message);
        }
        [Fact]
        public void AddDbContext_MultiplePostgresServices_ThrowsConnectorException()
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
        'p-mysql': [
        {
          'credentials': {
            'hostname': '192.168.0.80',
            'port': 3306,
            'name': 'cf_eabde00f_6383_4230_86df_98eb522bc87c',
            'username': '1solAZdtoCYfmjcj',
            'password': '7JmJzJgm4VH4ZkOh',
            'uri': 'mysql://1solAZdtoCYfmjcj:7JmJzJgm4VH4ZkOh@192.168.0.80:3306/cf_eabde00f_6383_4230_86df_98eb522bc87c?reconnect=true',
            'jdbcUrl': 'jdbc:mysql://192.168.0.80:3306/cf_eabde00f_6383_4230_86df_98eb522bc87c?user=1solAZdtoCYfmjcj&password=7JmJzJgm4VH4ZkOh'
          },
          'syslog_drain_url': null,
          'label': 'p-mysql',
          'provider': null,
          'plan': '100mb',
          'name': 'myMySqlService',
          'tags': [
            'mysql',
            'relational'
          ]
        },
        {
          'credentials': {
            'hostname': '192.168.0.80',
            'port': 3306,
            'name': 'cf_eabde00f_6383_4230_86df_98eb522bc87d',
            'username': '1solAZdtoCYfmjcj',
            'password': '7JmJzJgm4VH4ZkOh',
            'uri': 'mysql://1solAZdtoCYfmjcj:7JmJzJgm4VH4ZkOh@192.168.0.80:3306/cf_eabde00f_6383_4230_86df_98eb522bc87d?reconnect=true',
            'jdbcUrl': 'jdbc:mysql://192.168.0.80:3306/cf_eabde00f_6383_4230_86df_98eb522bc87d?user=1solAZdtoCYfmjcj&password=7JmJzJgm4VH4ZkOh'
          },
          'syslog_drain_url': null,
          'label': 'p-mysql',
          'provider': null,
          'plan': '100mb',
          'name': 'myMySqlService2',
          'tags': [
            'mysql',
            'relational'
          ]
        },
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
            services.AddDbContext<GoodDbContext>(options =>
                  options.UseMySql(config));

            var ex = Assert.Throws<ConnectorException>(() => services.BuildServiceProvider().GetService<GoodDbContext>());
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
        'p-mysql': [
        {
          'credentials': {
            'hostname': '192.168.0.80',
            'port': 3306,
            'name': 'cf_eabde00f_6383_4230_86df_98eb522bc87d',
            'username': '1solAZdtoCYfmjcj',
            'password': '7JmJzJgm4VH4ZkOh',
            'uri': 'mysql://1solAZdtoCYfmjcj:7JmJzJgm4VH4ZkOh@192.168.0.80:3306/cf_eabde00f_6383_4230_86df_98eb522bc87d?reconnect=true',
            'jdbcUrl': 'jdbc:mysql://192.168.0.80:3306/cf_eabde00f_6383_4230_86df_98eb522bc87d?user=1solAZdtoCYfmjcj&password=7JmJzJgm4VH4ZkOh'
          },
          'syslog_drain_url': null,
          'label': 'p-mysql',
          'provider': null,
          'plan': '100mb',
          'name': 'myMySqlService',
          'tags': [
            'mysql',
            'relational'
          ]
        },
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
            services.AddDbContext<GoodDbContext>(options =>
                  options.UseMySql(config));


            var built = services.BuildServiceProvider();
            var service = built.GetService<GoodDbContext>();
            Assert.NotNull(service);

            var con = service.Database.GetDbConnection();
            Assert.NotNull(con);
            var postCon = con as MySqlConnection;
            Assert.NotNull(postCon);

            var connString = con.ConnectionString;
            Assert.NotNull(connString);

            Assert.True(connString.Contains("cf_eabde00f_6383_4230_86df_98eb522bc87d"));
            Assert.True(connString.Contains("3306"));
            Assert.True(connString.Contains("192.168.0.80"));
            Assert.True(connString.Contains("1solAZdtoCYfmjcj"));
            Assert.True(connString.Contains("7JmJzJgm4VH4ZkOh"));

        }
    }

    class GoodDbContext : DbContext
    {
        public GoodDbContext(DbContextOptions<GoodDbContext> options) : base(options)
        {

        }
    }
}
#endif