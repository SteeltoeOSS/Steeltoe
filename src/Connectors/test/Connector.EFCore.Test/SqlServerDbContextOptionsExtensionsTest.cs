// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if !NET461 && !NETCOREAPP2_1
using Microsoft.Data.SqlClient;
#endif
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.CloudFoundry.Connector.EFCore.Test;
using Steeltoe.CloudFoundry.Connector.Test;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
#if NET461 || NETCOREAPP2_1
using System.Data.SqlClient;
#endif
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
        public void UseSqlServer_ThrowsIfConfigurationNull()
        {
            // Arrange
            var optionsBuilder = new DbContextOptionsBuilder();
            var goodBuilder = new DbContextOptionsBuilder<GoodDbContext>();
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
            var optionsBuilder = new DbContextOptionsBuilder();
            var goodBuilder = new DbContextOptionsBuilder<GoodDbContext>();
            var config = new ConfigurationBuilder().Build();
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
            var config = new ConfigurationBuilder().Build();

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
            var config = new ConfigurationBuilder().Build();

            // Act and Assert
            services.AddDbContext<GoodDbContext>(options => options.UseSqlServer(config, "foobar"));

            var ex = Assert.Throws<ConnectorException>(() => services.BuildServiceProvider().GetService<GoodDbContext>());
            Assert.Contains("foobar", ex.Message);
        }

        [Fact]
        public void AddDbContext_MultipleSqlServerServices_ThrowsConnectorException()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", SqlServerTestHelpers.TwoServerVCAP);

            var builder = new ConfigurationBuilder();
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
            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", SqlServerTestHelpers.SingleServerVCAP);

            var builder = new ConfigurationBuilder();
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

        [Fact]
        public void AddDbContexts_WithAzureVCAPs_AddsDbContexts()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", SqlServerTestHelpers.SingleServerAzureVCAP);

            var builder = new ConfigurationBuilder();
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
            Assert.Contains("Initial Catalog=u9e44b3e8e31", connString);
            Assert.Contains("Data Source=ud6893c77875.database.windows.net", connString);
            Assert.Contains("User Id=ud61c2c9ed2a", connString);
            Assert.Contains("Password=yNOaMnbsjGT3qk5eW6BXcbHE6b2Da8sLcao7SdIFFqA2q345jQ9RSw==", connString);
        }
    }
}
