// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Steeltoe.CloudFoundry.Connector.Relational;
using Steeltoe.Common.HealthChecks;
using System;
using System.Data;
using Xunit;

namespace Steeltoe.CloudFoundry.ConnectorAutofac.Test
{
    public class MySqlContainerBuilderExtensionsTest
    {
        [Fact]
        public void RegisterMySqlConnection_Requires_Builder()
        {
            // arrange
            IConfiguration config = new ConfigurationBuilder().Build();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => MySqlContainerBuilderExtensions.RegisterMySqlConnection(null, config));
        }

        [Fact]
        public void RegisterMySqlConnection_Requires_Config()
        {
            // arrange
            var cb = new ContainerBuilder();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => MySqlContainerBuilderExtensions.RegisterMySqlConnection(cb, null));
        }

        [Fact]
        public void RegisterMySqlConnection_AddsToContainer()
        {
            // arrange
            var container = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();

            // act
            var regBuilder = MySqlContainerBuilderExtensions.RegisterMySqlConnection(container, config);
            var services = container.Build();
            var dbConn = services.Resolve<IDbConnection>();

            // assert
            Assert.NotNull(dbConn);
            Assert.Equal(typeof(MySqlConnection).FullName, dbConn.GetType().FullName);
        }

        [Fact]
        public void RegisterMySqlConnection_AddsHealthContributorToContainer()
        {
            // arrange
            var container = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();

            // act
            var regBuilder = MySqlContainerBuilderExtensions.RegisterMySqlConnection(container, config);
            var services = container.Build();
            var healthContributor = services.Resolve<IHealthContributor>();

            // assert
            Assert.NotNull(healthContributor);
            Assert.IsType<RelationalHealthContributor>(healthContributor);
        }
    }
}
