// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connector.MySql;
using Steeltoe.Connector.Oracle;
using Steeltoe.Connector.PostgreSql;
using Steeltoe.Connector.Services;
using Steeltoe.Connector.SqlServer;
using System.Collections.Generic;
using System.Data;
using Xunit;

namespace Steeltoe.Connector.Relational.Test
{
    public class RelationalHealthContributorTest
    {
        [Fact]
        public void GetMySqlContributor_ReturnsContributor()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["mysql:client:server"] = "localhost",
                ["mysql:client:port"] = "1234",
                ["mysql:client:PersistSecurityInfo"] = "true",
                ["mysql:client:password"] = "password",
                ["mysql:client:username"] = "username"
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();
            var contrib = RelationalHealthContributor.GetMySqlContributor(config);
            Assert.NotNull(contrib);
            var status = contrib.Health();
            Assert.Equal(HealthStatus.DOWN, status.Status);
        }

        [Fact]
        public void GetPostgreSqlContributor_ReturnsContributor()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["postgres:client:host"] = "localhost",
                ["postgres:client:port"] = "1234",
                ["postgres:client:password"] = "password",
                ["postgres:client:username"] = "username"
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();
            var contrib = RelationalHealthContributor.GetPostgreSqlContributor(config);
            Assert.NotNull(contrib);
            var status = contrib.Health();
            Assert.Equal(HealthStatus.DOWN, status.Status);
        }

        [Fact]
        public void GetSqlServerContributor_ReturnsContributor()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["sqlserver:credentials:timeout"] = "1",
                ["sqlserver:credentials:uid"] = "username",
                ["sqlserver:credentials:uri"] = "jdbc:sqlserver://servername:1433;databaseName=de5aa3a747c134b3d8780f8cc80be519e",
                ["sqlserver:credentials:db"] = "de5aa3a747c134b3d8780f8cc80be519e",
                ["sqlserver:credentials:pw"] = "password"
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();
            var contrib = RelationalHealthContributor.GetSqlServerContributor(config);
            Assert.NotNull(contrib);
            var status = contrib.Health();
            Assert.Equal(HealthStatus.DOWN, status.Status);
        }

        [Fact]
        public void GetOracleContributor_ReturnsContributor()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["oracle:client:server"] = "localhost",
                ["oracle:client:port"] = "1234",
                ["oracle:client:PersistSecurityInfo"] = "true",
                ["oracle:client:password"] = "password",
                ["oracle:client:username"] = "username"
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();
            var contrib = RelationalHealthContributor.GetOracleContributor(config);
            Assert.NotNull(contrib);
            var status = contrib.Health();
            Assert.Equal(HealthStatus.DOWN, status.Status);
        }

        [Fact]
        public void Sql_Not_Connected_Returns_Down_Status()
        {
            // arrange
            var implementationType = SqlServerTypeLocator.SqlConnection;
            var sqlConfig = new SqlServerProviderConnectorOptions() { Timeout = 10 };
            var sInfo = new SqlServerServiceInfo("MyId", "jdbc:sqlserver://localhost:1433/databaseName=invalidDatabaseName", "Dd6O1BPXUHdrmzbP", "7E1LxXnlH2hhlPVt");
            var logrFactory = new LoggerFactory();
            var connFactory = new SqlServerProviderConnectorFactory(sInfo, sqlConfig, implementationType);
            var h = new RelationalHealthContributor((IDbConnection)connFactory.Create(null), logrFactory.CreateLogger<RelationalHealthContributor>());

            // act
            var status = h.Health();

            // assert
            Assert.Equal(HealthStatus.DOWN, status.Status);
            Assert.Contains(status.Details.Keys, k => k == "error");
        }

        [Fact(Skip = "Integration test - requires local db server")]
        public void Sql_Is_Connected_Returns_Up_Status()
        {
            // arrange
            var implementationType = SqlServerTypeLocator.SqlConnection;
            var sqlConfig = new SqlServerProviderConnectorOptions() { Timeout = 1, ConnectionString = "Server=(localdb)\\MSSQLLocalDB;Integrated Security=true" };
            var sInfo = new SqlServerServiceInfo("MyId", string.Empty);
            var logrFactory = new LoggerFactory();
            var connFactory = new SqlServerProviderConnectorFactory(sInfo, sqlConfig, implementationType);
            var h = new RelationalHealthContributor((IDbConnection)connFactory.Create(null), logrFactory.CreateLogger<RelationalHealthContributor>());

            // act
            var status = h.Health();

            // assert
            Assert.Equal(HealthStatus.UP, status.Status);
        }

        [Fact]
        public void MySql_Not_Connected_Returns_Down_Status()
        {
            // arrange
            var implementationType = MySqlTypeLocator.MySqlConnection;
            var sqlConfig = new MySqlProviderConnectorOptions() { ConnectionTimeout = 1 };
            var sInfo = new MySqlServiceInfo("MyId", "mysql://localhost:80;databaseName=invalidDatabaseName");
            var logrFactory = new LoggerFactory();
            var connFactory = new MySqlProviderConnectorFactory(sInfo, sqlConfig, implementationType);
            var h = new RelationalHealthContributor((IDbConnection)connFactory.Create(null), logrFactory.CreateLogger<RelationalHealthContributor>());

            // act
            var status = h.Health();

            // assert
            Assert.Equal(HealthStatus.DOWN, status.Status);
            Assert.Contains(status.Details.Keys, k => k == "error");
        }

        [Fact(Skip = "Integration test - requires local db server")]
        public void MySql_Is_Connected_Returns_Up_Status()
        {
            // arrange
            var implementationType = MySqlTypeLocator.MySqlConnection;
            var sqlConfig = new MySqlProviderConnectorOptions() { ConnectionTimeout = 1 };
            var sInfo = new MySqlServiceInfo("MyId", "mysql://steeltoe:steeltoe@localhost:3306");
            var logrFactory = new LoggerFactory();
            var connFactory = new MySqlProviderConnectorFactory(sInfo, sqlConfig, implementationType);
            var h = new RelationalHealthContributor((IDbConnection)connFactory.Create(null), logrFactory.CreateLogger<RelationalHealthContributor>());

            // act
            var status = h.Health();

            // assert
            Assert.Equal(HealthStatus.UP, status.Status);
        }

        [Fact]
        public void PostgreSql_Not_Connected_Returns_Down_Status()
        {
            // arrange
            var implementationType = PostgreSqlTypeLocator.NpgsqlConnection;
            var sqlConfig = new PostgresProviderConnectorOptions();
            var sInfo = new PostgresServiceInfo("MyId", "postgres://localhost:5432/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355");
            var logrFactory = new LoggerFactory();
            var connFactory = new PostgresProviderConnectorFactory(sInfo, sqlConfig, implementationType);
            var h = new RelationalHealthContributor((IDbConnection)connFactory.Create(null), logrFactory.CreateLogger<RelationalHealthContributor>());

            // act
            var status = h.Health();

            // assert
            Assert.Equal(HealthStatus.DOWN, status.Status);
            Assert.Contains(status.Details.Keys, k => k == "error");
        }

        [Fact(Skip = "Integration test - requires local db server")]
        public void PostgreSql_Is_Connected_Returns_Up_Status()
        {
            // arrange
            var implementationType = PostgreSqlTypeLocator.NpgsqlConnection;
            var sqlConfig = new PostgresProviderConnectorOptions();
            var sInfo = new PostgresServiceInfo("MyId", "postgres://steeltoe:steeltoe@localhost:5432/postgres");
            var logrFactory = new LoggerFactory();
            var connFactory = new PostgresProviderConnectorFactory(sInfo, sqlConfig, implementationType);
            var h = new RelationalHealthContributor((IDbConnection)connFactory.Create(null), logrFactory.CreateLogger<RelationalHealthContributor>());

            // act
            var status = h.Health();

            // assert
            Assert.Equal(HealthStatus.UP, status.Status);
        }

        [Fact]
        public void Oracle_Not_Connected_Returns_Down_Status()
        {
            // arrange
            var implementationType = OracleTypeLocator.OracleConnection;
            var sqlConfig = new OracleProviderConnectorOptions();
            var sInfo = new OracleServiceInfo("MyId", "oracle://user:pwd@localhost:1521/someService");
            var logrFactory = new LoggerFactory();
            var connFactory = new OracleProviderConnectorFactory(sInfo, sqlConfig, implementationType);
            var h = new RelationalHealthContributor((IDbConnection)connFactory.Create(null), logrFactory.CreateLogger<RelationalHealthContributor>());

            // act
            var status = h.Health();

            // assert
            Assert.Equal(HealthStatus.DOWN, status.Status);
            Assert.Contains(status.Details.Keys, k => k == "error");
        }

        [Fact(Skip = "Integration test - requires local db server")]
        public void Oracle_Is_Connected_Returns_Up_Status()
        {
            // arrange
            var implementationType = OracleTypeLocator.OracleConnection;
            var sqlConfig = new OracleProviderConnectorOptions();
            var sInfo = new OracleServiceInfo("MyId", "oracle://hr:hr@localhost:1521/orclpdb1");
            var logrFactory = new LoggerFactory();
            var connFactory = new OracleProviderConnectorFactory(sInfo, sqlConfig, implementationType);
            var h = new RelationalHealthContributor((IDbConnection)connFactory.Create(null), logrFactory.CreateLogger<RelationalHealthContributor>());

            // act
            var status = h.Health();

            // assert
            Assert.Equal(HealthStatus.UP, status.Status);
        }
    }
}
