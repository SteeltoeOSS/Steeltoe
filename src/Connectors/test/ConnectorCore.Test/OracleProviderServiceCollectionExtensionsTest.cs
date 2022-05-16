// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using Xunit;

namespace Steeltoe.Connector.Oracle.Test
{
    public class OracleProviderServiceCollectionExtensionsTest
    {
        [Fact]
        public void AddOracleConnection_ThrowsIfServiceCollectionNull()
        {
            IServiceCollection services = null;
            IConfigurationRoot config = null;

            var ex = Assert.Throws<ArgumentNullException>(() => services.AddOracleConnection(config));
            Assert.Contains(nameof(services), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddOracleConnection(config, "foobar"));
            Assert.Contains(nameof(services), ex2.Message);
        }

        [Fact]
        public void AddOracleConnection_ThrowsIfConfigurationNull()
        {
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;

            var ex = Assert.Throws<ArgumentNullException>(() => services.AddOracleConnection(config));
            Assert.Contains(nameof(config), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddOracleConnection(config, "foobar"));
            Assert.Contains(nameof(config), ex2.Message);
        }

        [Fact]
        public void AddOracleConnection_ThrowsIfServiceNameNull()
        {
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;
            string serviceName = null;

            var ex = Assert.Throws<ArgumentNullException>(() => services.AddOracleConnection(config, serviceName));
            Assert.Contains(nameof(serviceName), ex.Message);
        }

        [Fact]
        public void AddOracleConnection_AddsRelationalHealthContributor()
        {
            IServiceCollection services = new ServiceCollection();
            var builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            services.AddOracleConnection(config);
            var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RelationalDbHealthContributor;

            Assert.NotNull(healthContributor);
        }
    }
}
