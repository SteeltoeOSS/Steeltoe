// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector.MySql;
using Steeltoe.CloudFoundry.Connector.PostgreSql;
using Steeltoe.CloudFoundry.Connector.Relational;
using Steeltoe.CloudFoundry.Connector.Relational.MySql;
using Steeltoe.CloudFoundry.Connector.Relational.PostgreSql;
using Steeltoe.CloudFoundry.Connector.Relational.SqlServer;
using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.CloudFoundry.Connector.SqlServer;
using Steeltoe.Common.HealthChecks;
using System;
using System.Data;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Test.Relational
{
    public class RelationalHealthContributorTest
    {
        [Fact]
        public void Sql_Not_Connected_Returns_Down_Status()
        {
            // arrange
            Type implementationType = SqlServerTypeLocator.SqlConnection;
            var sqlConfig = new SqlServerProviderConnectorOptions() { Timeout = 1 };
            var sInfo = new SqlServerServiceInfo("MyId", "jdbc:sqlserver://localhost:1433;databaseName=invalidDatabaseName", "Dd6O1BPXUHdrmzbP", "7E1LxXnlH2hhlPVt");
            var logrFactory = new LoggerFactory();
            var connFactory = new SqlServerProviderConnectorFactory(sInfo, sqlConfig, implementationType);
            var h = new RelationalHealthContributor((IDbConnection)connFactory.Create(null), logrFactory.CreateLogger<IDbConnection>());

            // act
            var status = h.Health();

            // assert
            Assert.Equal(HealthStatus.DOWN, status.Status);
            Assert.Contains(status.Details.Keys, k => k == "error");
        }

        [Fact/*(Skip = "Integration test - requires local db server")*/]
        public void Sql_Is_Connected_Returns_Up_Status()
        {
            // arrange
            Type implementationType = SqlServerTypeLocator.SqlConnection;
            var sqlConfig = new SqlServerProviderConnectorOptions();
            var sInfo = new SqlServerServiceInfo("MyId", "jdbc:sqlserver://localhost:1433;databaseName=master", "steeltoe", "steeltoe");
            var logrFactory = new LoggerFactory();
            var connFactory = new SqlServerProviderConnectorFactory(sInfo, sqlConfig, implementationType);
            var h = new RelationalHealthContributor((IDbConnection)connFactory.Create(null), logrFactory.CreateLogger<IDbConnection>());

            // act
            var status = h.Health();

            // assert
            Assert.Equal(HealthStatus.UP, status.Status);
        }

        [Fact]
        public void MySql_Not_Connected_Returns_Down_Status()
        {
            // arrange
            Type implementationType = MySqlTypeLocator.MySqlConnection;
            var sqlConfig = new MySqlProviderConnectorOptions() { ConnectionTimeout = 1 };
            var sInfo = new MySqlServiceInfo("MyId", "mysql://localhost:80;databaseName=invalidDatabaseName");
            var logrFactory = new LoggerFactory();
            var connFactory = new MySqlProviderConnectorFactory(sInfo, sqlConfig, implementationType);
            var h = new RelationalHealthContributor((IDbConnection)connFactory.Create(null), logrFactory.CreateLogger<IDbConnection>());

            // act
            var status = h.Health();

            // assert
            Assert.Equal(HealthStatus.DOWN, status.Status);
            Assert.Contains(status.Details.Keys, k => k == "error");
        }

        [Fact/*(Skip = "Integration test - requires local db server")*/]
        public void MySql_Is_Connected_Returns_Up_Status()
        {
            // arrange
            Type implementationType = MySqlTypeLocator.MySqlConnection;
            var sqlConfig = new MySqlProviderConnectorOptions() { ConnectionTimeout = 1 };
            var sInfo = new MySqlServiceInfo("MyId", "mysql://steeltoe:steeltoe@localhost:3306");
            var logrFactory = new LoggerFactory();
            var connFactory = new MySqlProviderConnectorFactory(sInfo, sqlConfig, implementationType);
            var h = new RelationalHealthContributor((IDbConnection)connFactory.Create(null), logrFactory.CreateLogger<IDbConnection>());

            // act
            var status = h.Health();

            // assert
            Assert.Equal(HealthStatus.UP, status.Status);
        }

        [Fact]
        public void PostgreSql_Not_Connected_Returns_Down_Status()
        {
            // arrange
            Type implementationType = PostgreSqlTypeLocator.NpgsqlConnection;
            var sqlConfig = new PostgresProviderConnectorOptions();
            var sInfo = new PostgresServiceInfo("MyId", "postgres://localhost:5432/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355");
            var logrFactory = new LoggerFactory();
            var connFactory = new PostgresProviderConnectorFactory(sInfo, sqlConfig, implementationType);
            var h = new RelationalHealthContributor((IDbConnection)connFactory.Create(null), logrFactory.CreateLogger<IDbConnection>());

            // act
            var status = h.Health();

            // assert
            Assert.Equal(HealthStatus.DOWN, status.Status);
            Assert.Contains(status.Details.Keys, k => k == "error");
        }

        [Fact/*(Skip = "Integration test - requires local db server")*/]
        public void PostgreSql_Is_Connected_Returns_Up_Status()
        {
            // arrange
            Type implementationType = PostgreSqlTypeLocator.NpgsqlConnection;
            var sqlConfig = new PostgresProviderConnectorOptions();
            var sInfo = new PostgresServiceInfo("MyId", "postgres://steeltoe:steeltoe@localhost:5432/postgres");
            var logrFactory = new LoggerFactory();
            var connFactory = new PostgresProviderConnectorFactory(sInfo, sqlConfig, implementationType);
            var h = new RelationalHealthContributor((IDbConnection)connFactory.Create(null), logrFactory.CreateLogger<IDbConnection>());

            // act
            var status = h.Health();

            // assert
            Assert.Equal(HealthStatus.UP, status.Status);
        }
    }
}
