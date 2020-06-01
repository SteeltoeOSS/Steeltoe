// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
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
            Assert.Throws<ArgumentNullException>(() => SqlServerDbContextContainerBuilderExtensions.RegisterDbContext<GoodSqlServerDbContext>(null, config));
        }

        [Fact]
        public void RegisterMySqlDbContext_Requires_Config()
        {
            // arrange
            var cb = new ContainerBuilder();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => SqlServerDbContextContainerBuilderExtensions.RegisterDbContext<GoodSqlServerDbContext>(cb, null));
        }

        [Fact]
        public void RegisterMySqlDbContext_AddsToContainer()
        {
            // arrange
            var container = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();

            // act
            var regBuilder = SqlServerDbContextContainerBuilderExtensions.RegisterDbContext<GoodSqlServerDbContext>(container, config);
            var services = container.Build();
            var dbConn = services.Resolve<GoodSqlServerDbContext>();

            // assert
            Assert.NotNull(dbConn);
            Assert.IsType<GoodSqlServerDbContext>(dbConn);
        }
    }
}
