// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using System;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.EF6Autofac.Test
{
    public class MySqlDbContextContainerBuilderExtensionsTest
    {
        [Fact]
        public void RegisterMySqlDbContext_Requires_Builder()
        {
            // arrange
            IConfiguration config = new ConfigurationBuilder().Build();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => MySqlDbContextContainerBuilderExtensions.RegisterDbContext<GoodMySqlDbContext>(null, config));
        }

        [Fact]
        public void RegisterMySqlDbContext_Requires_Config()
        {
            // arrange
            var cb = new ContainerBuilder();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => MySqlDbContextContainerBuilderExtensions.RegisterDbContext<GoodMySqlDbContext>(cb, null));
        }

        [Fact]
        public void RegisterMySqlDbContext_AddsToContainer()
        {
            // arrange
            var container = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();

            // act
            var regBuilder = MySqlDbContextContainerBuilderExtensions.RegisterDbContext<GoodMySqlDbContext>(container, config);
            var services = container.Build();
            var dbConn = services.Resolve<GoodMySqlDbContext>();

            // assert
            Assert.NotNull(dbConn);
            Assert.IsType<GoodMySqlDbContext>(dbConn);
        }
    }
}
