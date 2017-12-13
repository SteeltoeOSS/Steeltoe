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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.CloudFoundry.Connector.Test;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Data.Entity;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.SqlServer.EF6.Test
{
    public class SqlServerDbContextServiceCollectionExtensionsTest
    {
        public SqlServerDbContextServiceCollectionExtensionsTest()
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
            var ex = Assert.Throws<ArgumentNullException>(() => SqlServerDbContextServiceCollectionExtensions.AddDbContext<GoodSqlServerDbContext>(services, config));
            Assert.Contains(nameof(services), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => SqlServerDbContextServiceCollectionExtensions.AddDbContext<GoodSqlServerDbContext>(services, config, "foobar"));
            Assert.Contains(nameof(services), ex2.Message);
        }

        [Fact]
        public void AddDbContext_ThrowsIfConfigurationNull()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => SqlServerDbContextServiceCollectionExtensions.AddDbContext<GoodSqlServerDbContext>(services, config));
            Assert.Contains(nameof(config), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => SqlServerDbContextServiceCollectionExtensions.AddDbContext<GoodSqlServerDbContext>(services, config, "foobar"));
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
            var ex = Assert.Throws<ArgumentNullException>(() => SqlServerDbContextServiceCollectionExtensions.AddDbContext<GoodSqlServerDbContext>(services, config, serviceName));
            Assert.Contains(nameof(serviceName), ex.Message);
        }

        [Fact]
        public void AddDbContext_NoVCAPs_AddsDbContext()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act and Assert
            SqlServerDbContextServiceCollectionExtensions.AddDbContext<GoodSqlServerDbContext>(services, config);

            var service = services.BuildServiceProvider().GetService<GoodSqlServerDbContext>();
            Assert.NotNull(service);
        }

        [Fact]
        public void AddDbContext_WithServiceName_NoVCAPs_ThrowsConnectorException()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act and Assert
            var ex = Assert.Throws<ConnectorException>(() => SqlServerDbContextServiceCollectionExtensions.AddDbContext<GoodSqlServerDbContext>(services, config, "foobar"));
            Assert.Contains("foobar", ex.Message);
        }

        [Fact]
        public void AddDbContext_MultipleSqlServerServices_ThrowsConnectorException()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", SqlServerTestHelpers.TwoServerVCAP);

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act and Assert
            var ex = Assert.Throws<ConnectorException>(() => SqlServerDbContextServiceCollectionExtensions.AddDbContext<GoodSqlServerDbContext>(services, config));
            Assert.Contains("Multiple", ex.Message);
        }

        [Fact]
        public void AddDbContexts_WithVCAPs_AddsDbContexts()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", SqlServerTestHelpers.SingleServerVCAP);

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act and Assert
            SqlServerDbContextServiceCollectionExtensions.AddDbContext<GoodSqlServerDbContext>(services, config);
            SqlServerDbContextServiceCollectionExtensions.AddDbContext<Good2SqlServerDbContext>(services, config);

            var built = services.BuildServiceProvider();
            var service = built.GetService<GoodSqlServerDbContext>();
            Assert.NotNull(service);

            var service2 = built.GetService<Good2SqlServerDbContext>();
            Assert.NotNull(service2);
        }
    }
}
