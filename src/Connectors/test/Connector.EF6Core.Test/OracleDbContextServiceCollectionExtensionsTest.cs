// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.CloudFoundry.Connector.EF6Core;
using Steeltoe.CloudFoundry.Connector.Relational;
using Steeltoe.Common.HealthChecks;
using System;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Oracle.EF6.Test
{
    public class OracleDbContextServiceCollectionExtensionsTest
    {
        [Fact]
        public void AddDbContext_ThrowsIfServiceCollectionNull()
        {
            // Arrange
            IServiceCollection services = null;
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => OracleDbContextServiceCollectionExtensions.AddDbContext<GoodOracleDbContext>(services, config));
            Assert.Contains(nameof(services), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => OracleDbContextServiceCollectionExtensions.AddDbContext<GoodOracleDbContext>(services, config, "foobar"));
            Assert.Contains(nameof(services), ex2.Message);
        }

        [Fact]
        public void AddDbContext_ThrowsIfConfigurationNull()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => OracleDbContextServiceCollectionExtensions.AddDbContext<GoodOracleDbContext>(services, config));
            Assert.Contains(nameof(config), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => OracleDbContextServiceCollectionExtensions.AddDbContext<GoodOracleDbContext>(services, config, "foobar"));
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
            var ex = Assert.Throws<ArgumentNullException>(() => OracleDbContextServiceCollectionExtensions.AddDbContext<GoodOracleDbContext>(services, config, serviceName));
            Assert.Contains(nameof(serviceName), ex.Message);
        }

        [Fact]
        public void AddDbContext_NoVCAPs_AddsDbContext()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();

            // Act and Assert
            services.AddDbContext<GoodOracleDbContext>(config);

            var serviceProvider = services.BuildServiceProvider();
            var service = serviceProvider.GetService<GoodOracleDbContext>();
            var serviceHealth = serviceProvider.GetService<IHealthContributor>();
            Assert.NotNull(service);
#if NET461
            Assert.NotNull(serviceHealth);
            Assert.IsAssignableFrom<RelationalHealthContributor>(serviceHealth);
#else
            Assert.Null(serviceHealth);
#endif
        }
    }
}
