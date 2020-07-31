// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Steeltoe.CloudFoundry.Connector.Relational;
using Steeltoe.Common.HealthChecks;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.CloudFoundry.ConnectorAutofac.Test
{
    public class PostgreSqlContainerBuilderExtensionsTest
    {
        [Fact]
        public void RegisterPostgreSqlConnection_Requires_Builder()
        {
            // arrange
            IConfiguration config = new ConfigurationBuilder().Build();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => PostgreSqlContainerBuilderExtensions.RegisterPostgreSqlConnection(null, config));
        }

        [Fact]
        public void RegisterPostgreSqlConnection_Requires_Config()
        {
            // arrange
            var cb = new ContainerBuilder();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => PostgreSqlContainerBuilderExtensions.RegisterPostgreSqlConnection(cb, null));
        }

        [Fact]
        public void RegisterPostgreSqlConnection_AddsToContainer()
        {
            // arrange
            var container = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();

            // act
            var regBuilder = PostgreSqlContainerBuilderExtensions.RegisterPostgreSqlConnection(container, config);
            var services = container.Build();
            var dbConn = services.Resolve<IDbConnection>();

            // assert
            Assert.NotNull(dbConn);
            Assert.IsType<NpgsqlConnection>(dbConn);
        }

        [Fact]
        public void RegisterPostgreSqlConnection_AddsHealthContributorToContainer()
        {
            // arrange
            var container = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();

            // act
            var regBuilder = PostgreSqlContainerBuilderExtensions.RegisterPostgreSqlConnection(container, config);
            var services = container.Build();
            var healthContributor = services.Resolve<IHealthContributor>();

            // assert
            Assert.NotNull(healthContributor);
            Assert.IsType<RelationalHealthContributor>(healthContributor);
        }
    }
}
