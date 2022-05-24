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
    public class OracleServiceCollectionExtensionsTest
    {
        [Fact]
        public void AddOracleHealthContributor_ThrowsIfServiceCollectionNull()
        {
            const IServiceCollection services = null;
            const IConfigurationRoot config = null;

            var ex = Assert.Throws<ArgumentNullException>(() => services.AddOracleHealthContributor(config));
            Assert.Contains(nameof(services), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddOracleHealthContributor(config, "foobar"));
            Assert.Contains(nameof(services), ex2.Message);
        }

        [Fact]
        public void AddOracleHealthContributor_ThrowsIfConfigurationNull()
        {
            IServiceCollection services = new ServiceCollection();
            const IConfigurationRoot config = null;

            var ex = Assert.Throws<ArgumentNullException>(() => services.AddOracleHealthContributor(config));
            Assert.Contains(nameof(config), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddOracleHealthContributor(config, "foobar"));
            Assert.Contains(nameof(config), ex2.Message);
        }

        [Fact]
        public void AddOracleHealthContributor_ThrowsIfServiceNameNull()
        {
            IServiceCollection services = new ServiceCollection();
            const IConfigurationRoot config = null;
            const string serviceName = null;

            var ex = Assert.Throws<ArgumentNullException>(() => services.AddOracleHealthContributor(config, serviceName));
            Assert.Contains(nameof(serviceName), ex.Message);
        }

        [Fact]
        public void AddOracleHealthContributor_AddsRelationalHealthContributor()
        {
            IServiceCollection services = new ServiceCollection();
            var builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            services.AddOracleHealthContributor(config);
            var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RelationalDbHealthContributor;

            Assert.NotNull(healthContributor);
        }
    }
}
