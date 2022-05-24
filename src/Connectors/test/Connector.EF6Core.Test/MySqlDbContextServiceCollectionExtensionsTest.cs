// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using Xunit;

namespace Steeltoe.Connector.MySql.EF6.Test
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
            const IServiceCollection services = null;
            const IConfigurationRoot config = null;

            var ex = Assert.Throws<ArgumentNullException>(() => services.AddDbContext<GoodMySqlDbContext>(config));
            Assert.Contains(nameof(services), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddDbContext<GoodMySqlDbContext>(config, "foobar"));
            Assert.Contains(nameof(services), ex2.Message);
        }

        [Fact]
        public void AddDbContext_ThrowsIfConfigurationNull()
        {
            IServiceCollection services = new ServiceCollection();
            const IConfigurationRoot config = null;

            var ex = Assert.Throws<ArgumentNullException>(() => services.AddDbContext<GoodMySqlDbContext>(config));
            Assert.Contains(nameof(config), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddDbContext<GoodMySqlDbContext>(config, "foobar"));
            Assert.Contains(nameof(config), ex2.Message);
        }

        [Fact]
        public void AddDbContext_ThrowsIfServiceNameNull()
        {
            IServiceCollection services = new ServiceCollection();
            const IConfigurationRoot config = null;
            const string serviceName = null;

            var ex = Assert.Throws<ArgumentNullException>(() => services.AddDbContext<GoodMySqlDbContext>(config, serviceName));
            Assert.Contains(nameof(serviceName), ex.Message);
        }

        [Fact]
        public void AddDbContext_NoVCAPs_AddsDbContext()
        {
            IServiceCollection services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();

            services.AddDbContext<GoodMySqlDbContext>(config);

            var serviceProvider = services.BuildServiceProvider();
            var service = serviceProvider.GetService<GoodMySqlDbContext>();
            var serviceHealth = serviceProvider.GetService<IHealthContributor>();
            Assert.NotNull(service);
            Assert.NotNull(serviceHealth);
            Assert.IsAssignableFrom<RelationalDbHealthContributor>(serviceHealth);
        }

        [Fact]
        public void AddDbContext_WithServiceName_NoVCAPs_ThrowsConnectorException()
        {
            IServiceCollection services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();

            var ex = Assert.Throws<ConnectorException>(() => services.AddDbContext<GoodMySqlDbContext>(config, "foobar"));
            Assert.Contains("foobar", ex.Message);
        }

        [Fact]
        public void AddDbContext_MultipleMySqlServices_ThrowsConnectorException()
        {
            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", MySqlTestHelpers.TwoServerVCAP);

            var builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            var ex = Assert.Throws<ConnectorException>(() => services.AddDbContext<GoodMySqlDbContext>(config));
            Assert.Contains("Multiple", ex.Message);
        }

        [Fact]
        public void AddDbContexts_WithVCAPs_AddsDbContexts()
        {
            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", MySqlTestHelpers.SingleServerVCAP);

            var builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            services.AddDbContext<GoodMySqlDbContext>(config);
            services.AddDbContext<Good2MySqlDbContext>(config);

            var built = services.BuildServiceProvider();
            var service = built.GetService<GoodMySqlDbContext>();
            Assert.NotNull(service);

            var service2 = built.GetService<Good2MySqlDbContext>();
            Assert.NotNull(service2);
        }
    }
}