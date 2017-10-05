// Copyright 2017 the original author or authors.
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

#if !NET461
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Extensions.Configuration;
using System;
using System.Data.SqlClient;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.SqlServer.EFCore.Test
{
    public class SqlServerDbContextOptionsExtensionsTest
    {
        public SqlServerDbContextOptionsExtensionsTest()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
        }

        [Fact]
        public void UseSqlServer_ThrowsIfDbContextOptionsBuilderNull()
        {
            // Arrange
            DbContextOptionsBuilder optionsBuilder = null;
            DbContextOptionsBuilder<GoodDbContext> goodBuilder = null;
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => SqlServerDbContextOptionsExtensions.UseSqlServer(optionsBuilder, config));
            Assert.Contains(nameof(optionsBuilder), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => SqlServerDbContextOptionsExtensions.UseSqlServer(optionsBuilder, config, "foobar"));
            Assert.Contains(nameof(optionsBuilder), ex2.Message);

            var ex3 = Assert.Throws<ArgumentNullException>(() => SqlServerDbContextOptionsExtensions.UseSqlServer<GoodDbContext>(goodBuilder, config));
            Assert.Contains(nameof(optionsBuilder), ex3.Message);

            var ex4 = Assert.Throws<ArgumentNullException>(() => SqlServerDbContextOptionsExtensions.UseSqlServer<GoodDbContext>(goodBuilder, config, "foobar"));
            Assert.Contains(nameof(optionsBuilder), ex4.Message);
        }

        [Fact]
        public void UseSqlServer_ThrowsIfConfigurtionNull()
        {
            // Arrange
            DbContextOptionsBuilder optionsBuilder = new DbContextOptionsBuilder();
            DbContextOptionsBuilder<GoodDbContext> goodBuilder = new DbContextOptionsBuilder<GoodDbContext>();
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => SqlServerDbContextOptionsExtensions.UseSqlServer(optionsBuilder, config));
            Assert.Contains(nameof(config), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => SqlServerDbContextOptionsExtensions.UseSqlServer(optionsBuilder, config, "foobar"));
            Assert.Contains(nameof(config), ex2.Message);

            var ex3 = Assert.Throws<ArgumentNullException>(() => SqlServerDbContextOptionsExtensions.UseSqlServer<GoodDbContext>(goodBuilder, config));
            Assert.Contains(nameof(config), ex3.Message);

            var ex4 = Assert.Throws<ArgumentNullException>(() => SqlServerDbContextOptionsExtensions.UseSqlServer<GoodDbContext>(goodBuilder, config, "foobar"));
            Assert.Contains(nameof(config), ex4.Message);
        }

        [Fact]
        public void UseSqlServer_ThrowsIfServiceNameNull()
        {
            // Arrange
            DbContextOptionsBuilder optionsBuilder = new DbContextOptionsBuilder();
            DbContextOptionsBuilder<GoodDbContext> goodBuilder = new DbContextOptionsBuilder<GoodDbContext>();
            IConfigurationRoot config = new ConfigurationBuilder().Build();
            string serviceName = null;

            // Act and Assert
            var ex2 = Assert.Throws<ArgumentException>(() => SqlServerDbContextOptionsExtensions.UseSqlServer(optionsBuilder, config, serviceName));
            Assert.Contains(nameof(serviceName), ex2.Message);

            var ex4 = Assert.Throws<ArgumentException>(() => SqlServerDbContextOptionsExtensions.UseSqlServer<GoodDbContext>(goodBuilder, config, serviceName));
            Assert.Contains(nameof(serviceName), ex4.Message);
        }

        [Fact]
        public void AddDbContext_NoVCAPs_AddsDbContext_WithSqlServerConnection()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act and Assert
            services.AddDbContext<GoodDbContext>(options => options.UseSqlServer(config));

            var service = services.BuildServiceProvider().GetService<GoodDbContext>();
            Assert.NotNull(service);
            var con = service.Database.GetDbConnection();
            Assert.NotNull(con);
            Assert.IsType<SqlConnection>(con);
        }

        [Fact]
        public void AddDbContext_WithServiceName_NoVCAPs_ThrowsConnectorException()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act and Assert
            services.AddDbContext<GoodDbContext>(options => options.UseSqlServer(config, "foobar"));

            var ex = Assert.Throws<ConnectorException>(() => services.BuildServiceProvider().GetService<GoodDbContext>());
            Assert.Contains("foobar", ex.Message);
        }

        [Fact]
        public void AddDbContext_MultipleSqlServerServices_ThrowsConnectorException()
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
                        }";
            var env2 = @"
                        {
                            'SqlServer': [
                                {
                                    'credentials': {
                                        'uid': 'uf33b2b30783a4087948c30f6c3b0c90f',
                                        'uri': 'jdbc:sqlserver://192.168.0.80:1433;databaseName=db1',
                                        'db': 'de5aa3a747c134b3d8780f8cc80be519e',
                                        'pw': 'Pefbb929c1e0945b5bab5b8f0d110c503'
                                    },
                                    'syslog_drain_url': null,
                                    'label': 'SqlServer',
                                    'provider': null,
                                    'plan': 'sharedVM',
                                    'name': 'mySqlServerService',
                                    'tags': [
                                        'sqlserver'
                                    ]
                                },
                                {
                                    'credentials': {
                                        'uid': 'uf33b2b30783a4087948c30f6c3b0c90f',
                                        'uri': 'jdbc:sqlserver://192.168.0.80:1433;databaseName=db2',
                                        'db': 'de5aa3a747c134b3d8780f8cc80be519e',
                                        'pw': 'Pefbb929c1e0945b5bab5b8f0d110c503'
                                    },
                                    'syslog_drain_url': null,
                                    'label': 'SqlServer',
                                    'provider': null,
                                    'plan': 'sharedVM',
                                    'name': 'mySqlServerService',
                                    'tags': [
                                        'sqlserver'
                                    ]
                                },
                            ]
                        }";

            // Arrange
            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", env1);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", env2);

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act and Assert
            services.AddDbContext<GoodDbContext>(options =>
                  options.UseSqlServer(config));

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
                        }";
            var env2 = @"
                        {
                            'SqlServer': [
                                {
                                    'credentials': {
                                        'uid': 'uf33b2b30783a4087948c30f6c3b0c90f',
                                        'uri': 'jdbc:sqlserver://192.168.0.80:1433;databaseName=de5aa3a747c134b3d8780f8cc80be519e',
                                        'db': 'de5aa3a747c134b3d8780f8cc80be519e',
                                        'pw': 'Pefbb929c1e0945b5bab5b8f0d110c503'
                                    },
                                    'syslog_drain_url': null,
                                    'label': 'SqlServer',
                                    'provider': null,
                                    'plan': 'sharedVM',
                                    'name': 'mySqlServerService',
                                    'tags': [
                                        'sqlserver'
                                    ]
                                },
                            ]
                        }";

            // Arrange
            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", env1);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", env2);

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act and Assert
            services.AddDbContext<GoodDbContext>(options => options.UseSqlServer(config));

            var built = services.BuildServiceProvider();
            var service = built.GetService<GoodDbContext>();
            Assert.NotNull(service);

            var con = service.Database.GetDbConnection();
            Assert.NotNull(con);
            Assert.IsType<SqlConnection>(con);

            var connString = con.ConnectionString;
            Assert.NotNull(connString);
            Assert.Contains("Initial Catalog=de5aa3a747c134b3d8780f8cc80be519e", connString);
            Assert.Contains("Data Source=192.168.0.80", connString);
            Assert.Contains("User Id=uf33b2b30783a4087948c30f6c3b0c90f", connString);
            Assert.Contains("Password=Pefbb929c1e0945b5bab5b8f0d110c503", connString);
        }
    }

    class GoodDbContext : DbContext
    {
        public GoodDbContext(DbContextOptions<GoodDbContext> options)
            : base(options)
        {
        }
    }
}
#endif