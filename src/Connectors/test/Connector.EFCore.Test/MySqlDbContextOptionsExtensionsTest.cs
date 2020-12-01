// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
#if NETCOREAPP3_1
using MySql.Data.MySqlClient;
#else
using MySqlConnector;
#endif
using Steeltoe.Connector.EFCore.Test;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Connector.MySql.EFCore.Test
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
        public void UseMySql_ThrowsIfConfigurationNull()
        {
            // Arrange
            var optionsBuilder = new DbContextOptionsBuilder();
            var goodBuilder = new DbContextOptionsBuilder<GoodDbContext>();
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
            var optionsBuilder = new DbContextOptionsBuilder();
            var goodBuilder = new DbContextOptionsBuilder<GoodDbContext>();
            var config = new ConfigurationBuilder().Build();
            string serviceName = null;

            // Act and Assert
            var ex2 = Assert.Throws<ArgumentException>(() => MySqlDbContextOptionsExtensions.UseMySql(optionsBuilder, config, serviceName));
            Assert.Contains(nameof(serviceName), ex2.Message);

            var ex4 = Assert.Throws<ArgumentException>(() => MySqlDbContextOptionsExtensions.UseMySql<GoodDbContext>(goodBuilder, config, serviceName));
            Assert.Contains(nameof(serviceName), ex4.Message);
        }

        [Fact]
        public void AddDbContext_NoVCAPs_AddsDbContext_WithMySqlConnection()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();

            // Act and Assert
#if NETCOREAPP3_1
            services.AddDbContext<GoodDbContext>(options => options.UseMySql(config));
#else
            services.AddDbContext<GoodDbContext>(options => options.UseMySql(config, serverVersion: MySqlServerVersion.LatestSupportedServerVersion));
#endif

            var service = services.BuildServiceProvider().GetService<GoodDbContext>();
            Assert.NotNull(service);
            var con = service.Database.GetDbConnection();
            Assert.NotNull(con);
            Assert.NotNull(con as MySqlConnection);
        }

#if NET5_0
        // Run a MySQL server with Docker to match creds below with this command
        // docker run --name steeltoe-mysql -p 3306:3306 -e MYSQL_DATABASE=steeltoe -e MYSQL_ROOT_PASSWORD=steeltoe mysql
        [Fact(Skip = "Requires a running MySQL server to support AutoDetect")]
        public void AddDbContext_NoVCAPs_AddsDbContext_WithMySqlConnection_AutodetectOn5_0()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { { "mysql:client:database", "steeltoe2" }, { "mysql:client:username", "root" }, { "mysql:client:password", "steeltoe" } }).Build();

            // Act and Assert
            services.AddDbContext<GoodDbContext>(options => options.UseMySql(config));

            var service = services.BuildServiceProvider().GetService<GoodDbContext>();
            Assert.NotNull(service);
            var con = service.Database.GetDbConnection();
            Assert.NotNull(con);
            Assert.NotNull(con as MySqlConnection);
        }
#endif

        [Fact]
        public void AddDbContext_WithServiceName_NoVCAPs_ThrowsConnectorException()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();

            // Act
            services.AddDbContext<GoodDbContext>(options => options.UseMySql(config, "foobar"));

            // Assert
            var ex = Assert.Throws<ConnectorException>(() => services.BuildServiceProvider().GetService<GoodDbContext>());
            Assert.Contains("foobar", ex.Message);
        }

        [Fact]
        public void AddDbContext_MultipleMySqlServices_ThrowsConnectorException()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", MySqlTestHelpers.TwoServerVCAP);

            var builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act
            services.AddDbContext<GoodDbContext>(options => options.UseMySql(config));

            // Assert
            var ex = Assert.Throws<ConnectorException>(() => services.BuildServiceProvider().GetService<GoodDbContext>());
            Assert.Contains("Multiple", ex.Message);
        }

        [Fact]
        public void AddDbContext_MultipleMySqlServices_AddWithName_Adds()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", MySqlTestHelpers.TwoServerVCAP);

            var builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act
#if NETCOREAPP3_1
            services.AddDbContext<GoodDbContext>(options => options.UseMySql(config, "spring-cloud-broker-db2"));
#else
            services.AddDbContext<GoodDbContext>(options => options.UseMySql(config, "spring-cloud-broker-db2", serverVersion: MySqlServerVersion.LatestSupportedServerVersion));
#endif

            // Assert
            var built = services.BuildServiceProvider();
            var service = built.GetService<GoodDbContext>();
            Assert.NotNull(service);

            var con = service.Database.GetDbConnection();
            Assert.NotNull(con);
            Assert.IsType<MySqlConnection>(con);

            var connString = con.ConnectionString;
            Assert.NotNull(connString);
            Assert.Contains("Server=192.168.0.91", connString, StringComparison.InvariantCultureIgnoreCase);
            Assert.Contains("Port=3306", connString, StringComparison.InvariantCultureIgnoreCase);
            Assert.Contains("Database=cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd0407903550", connString, StringComparison.InvariantCultureIgnoreCase);
            Assert.Contains("User Id=Dd6O1BPXUHdrmzbP0", connString, StringComparison.InvariantCultureIgnoreCase);
            Assert.Contains("Password=7E1LxXnlH2hhlPVt0", connString, StringComparison.InvariantCultureIgnoreCase);
        }

        [Fact]
        public void AddDbContexts_WithVCAPs_AddsDbContexts()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", MySqlTestHelpers.SingleServerVCAP);

            var builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act and Assert
#if NETCOREAPP3_1
            services.AddDbContext<GoodDbContext>(options => options.UseMySql(config));
#else
            services.AddDbContext<GoodDbContext>(options => options.UseMySql(config, serverVersion: MySqlServerVersion.LatestSupportedServerVersion));
#endif

            var built = services.BuildServiceProvider();
            var service = built.GetService<GoodDbContext>();
            Assert.NotNull(service);

            var con = service.Database.GetDbConnection();
            Assert.NotNull(con);
            Assert.IsType<MySqlConnection>(con);

            var connString = con.ConnectionString;
            Assert.NotNull(connString);
            Assert.Contains("Server=192.168.0.90", connString, StringComparison.InvariantCultureIgnoreCase);
            Assert.Contains("Port=3306", connString, StringComparison.InvariantCultureIgnoreCase);
            Assert.Contains("Database=cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", connString, StringComparison.InvariantCultureIgnoreCase);
            Assert.Contains("User Id=Dd6O1BPXUHdrmzbP", connString, StringComparison.InvariantCultureIgnoreCase);
            Assert.Contains("Password=7E1LxXnlH2hhlPVt", connString, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
