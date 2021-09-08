// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Steeltoe.CloudFoundry.Connector.Relational;
using Steeltoe.Common.HealthChecks;
using System;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.EF6Autofac.Test
{
    public class SqlServerDbContextContainerBuilderExtensionsTest
    {
        [Fact]
        public void RegisterSqlServerDbContext_Requires_Builder()
        {
            // arrange
            IConfiguration config = new ConfigurationBuilder().Build();

            // act & assert
            var ex = Assert.Throws<ArgumentNullException>(() => SqlServerDbContextContainerBuilderExtensions.RegisterSqlServerDbContext<GoodSqlServerDbContext>(null, config));
            Assert.Equal("container", ex.ParamName);
        }

        [Fact]
        public void RegisterSqlServerDbContext_Requires_Config()
        {
            // arrange
            var cb = new ContainerBuilder();

            // act & assert
            var ex = Assert.Throws<ArgumentNullException>(() => SqlServerDbContextContainerBuilderExtensions.RegisterSqlServerDbContext<GoodSqlServerDbContext>(cb, null));
            Assert.Equal("config", ex.ParamName);
        }

        [Fact]
        public void RegisterSqlServerDbContext_AddsToContainer()
        {
            // arrange
            var container = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();

            // act
            var regBuilder = container.RegisterSqlServerDbContext<GoodSqlServerDbContext>(config);
            var services = container.Build();
            var dbConn = services.Resolve<GoodSqlServerDbContext>();
            var health = services.Resolve<IHealthContributor>();

            // assert
            Assert.NotNull(dbConn);
            Assert.IsType<GoodSqlServerDbContext>(dbConn);
            Assert.NotNull(health);
            Assert.IsType<RelationalHealthContributor>(health);
        }
    }
}
