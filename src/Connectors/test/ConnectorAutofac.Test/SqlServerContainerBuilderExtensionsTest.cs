// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Steeltoe.CloudFoundry.Connector.Relational;
using Steeltoe.Common.HealthChecks;
using System;
using System.Data;
using System.Data.SqlClient;
using Xunit;

namespace Steeltoe.CloudFoundry.ConnectorAutofac.Test
{
    public class SqlServerContainerBuilderExtensionsTest
    {
        [Fact]
        public void RegisterSqlServerConnection_Requires_Builder()
        {
            // arrange
            IConfiguration config = new ConfigurationBuilder().Build();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => SqlServerContainerBuilderExtensions.RegisterSqlServerConnection(null, config));
        }

        [Fact]
        public void RegisterSqlServerConnection_Requires_Config()
        {
            // arrange
            var cb = new ContainerBuilder();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => SqlServerContainerBuilderExtensions.RegisterSqlServerConnection(cb, null));
        }

        [Fact]
        public void RegisterSqlServerConnection_AddsToContainer()
        {
            // arrange
            var container = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();

            // act
            var regBuilder = SqlServerContainerBuilderExtensions.RegisterSqlServerConnection(container, config);
            var services = container.Build();
            var dbConn = services.Resolve<IDbConnection>();

            // assert
            Assert.NotNull(dbConn);
            Assert.IsType<SqlConnection>(dbConn);
        }

        [Fact]
        public void RegisterSqlServerConnection_AddsHealthContributorToContainer()
        {
            // arrange
            var container = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();

            // act
            var regBuilder = SqlServerContainerBuilderExtensions.RegisterSqlServerConnection(container, config);
            var services = container.Build();
            var healthContributor = services.Resolve<IHealthContributor>();

            // assert
            Assert.NotNull(healthContributor);
            Assert.IsType<RelationalHealthContributor>(healthContributor);
        }
    }
}
