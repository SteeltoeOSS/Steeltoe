// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

namespace Steeltoe.Connector.Test;

public class RelationalDbHealthContributorTest
{
    [Fact]
    public void GetMySqlContributor_ReturnsContributor()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["mysql:client:server"] = "localhost",
            ["mysql:client:port"] = "1234",
            ["mysql:client:PersistSecurityInfo"] = "true",
            ["mysql:client:password"] = "password",
            ["mysql:client:username"] = "username",
            ["mysql:client:ConnectionTimeout"] = "1"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        var config = configurationBuilder.Build();
        var contrib = RelationalDbHealthContributor.GetMySqlContributor(config);
        Assert.NotNull(contrib);
        var status = contrib.Health();
        Assert.Equal(HealthStatus.Down, status.Status);
    }

    [Fact]
    public void GetPostgreSqlContributor_ReturnsContributor()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["postgres:client:host"] = "localhost",
            ["postgres:client:port"] = "1234",
            ["postgres:client:password"] = "password",
            ["postgres:client:username"] = "username",
            ["postgres:client:timeout"] = "1"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        var config = configurationBuilder.Build();
        var contrib = RelationalDbHealthContributor.GetPostgreSqlContributor(config);
        Assert.NotNull(contrib);
        var status = contrib.Health();
        Assert.Equal(HealthStatus.Down, status.Status);
    }

    [Fact]
    public void GetSqlServerContributor_ReturnsContributor()
    {
        var appsettings = new Dictionary<string, string>
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
        var contrib = RelationalDbHealthContributor.GetSqlServerContributor(config);
        Assert.NotNull(contrib);
        var status = contrib.Health();
        Assert.Equal(HealthStatus.Down, status.Status);
    }

    [Fact]
    public void GetOracleContributor_ReturnsContributor()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["oracle:client:server"] = "localhost",
            ["oracle:client:port"] = "1234",
            ["oracle:client:PersistSecurityInfo"] = "true",
            ["oracle:client:password"] = "password",
            ["oracle:client:username"] = "username",
            ["oracle:client:connectiontimeout"] = "1"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        var config = configurationBuilder.Build();
        var contrib = RelationalDbHealthContributor.GetOracleContributor(config);
        Assert.NotNull(contrib);
        var status = contrib.Health();
        Assert.Equal(HealthStatus.Down, status.Status);
    }

    [Fact]
    public void Sql_Not_Connected_Returns_Down_Status()
    {
        var implementationType = SqlServerTypeLocator.SqlConnection;
        var sqlConfig = new SqlServerProviderConnectorOptions { Timeout = 1 };
        var sInfo = new SqlServerServiceInfo("MyId", "jdbc:sqlserver://localhost:1433/databaseName=invalidDatabaseName", "Dd6O1BPXUHdrmzbP", "7E1LxXnlH2hhlPVt");
        var factory = new LoggerFactory();
        var connFactory = new SqlServerProviderConnectorFactory(sInfo, sqlConfig, implementationType);
        var h = new RelationalDbHealthContributor((IDbConnection)connFactory.Create(null), factory.CreateLogger<RelationalDbHealthContributor>());

        var status = h.Health();

        Assert.Equal(HealthStatus.Down, status.Status);
        Assert.Contains(status.Details.Keys, k => k == "error");
    }

    [Fact(Skip = "Integration test - requires local db server")]
    public void Sql_Is_Connected_Returns_Up_Status()
    {
        var implementationType = SqlServerTypeLocator.SqlConnection;
        var sqlConfig = new SqlServerProviderConnectorOptions { Timeout = 1, ConnectionString = "Server=(localdb)\\MSSQLLocalDB;Integrated Security=true" };
        var sInfo = new SqlServerServiceInfo("MyId", string.Empty);
        var factory = new LoggerFactory();
        var connFactory = new SqlServerProviderConnectorFactory(sInfo, sqlConfig, implementationType);
        var h = new RelationalDbHealthContributor((IDbConnection)connFactory.Create(null), factory.CreateLogger<RelationalDbHealthContributor>());

        var status = h.Health();

        Assert.Equal(HealthStatus.Up, status.Status);
    }

    [Fact]
    public void MySql_Not_Connected_Returns_Down_Status()
    {
        var implementationType = MySqlTypeLocator.MySqlConnection;
        var sqlConfig = new MySqlProviderConnectorOptions { ConnectionTimeout = 1 };
        var sInfo = new MySqlServiceInfo("MyId", "mysql://localhost:80;databaseName=invalidDatabaseName");
        var factory = new LoggerFactory();
        var connFactory = new MySqlProviderConnectorFactory(sInfo, sqlConfig, implementationType);
        var h = new RelationalDbHealthContributor((IDbConnection)connFactory.Create(null), factory.CreateLogger<RelationalDbHealthContributor>());

        var status = h.Health();

        Assert.Equal(HealthStatus.Down, status.Status);
        Assert.Contains(status.Details.Keys, k => k == "error");
    }

    [Fact(Skip = "Integration test - requires local db server")]
    public void MySql_Is_Connected_Returns_Up_Status()
    {
        var implementationType = MySqlTypeLocator.MySqlConnection;
        var sqlConfig = new MySqlProviderConnectorOptions { ConnectionTimeout = 1 };
        var sInfo = new MySqlServiceInfo("MyId", "mysql://steeltoe:steeltoe@localhost:3306");
        var factory = new LoggerFactory();
        var connFactory = new MySqlProviderConnectorFactory(sInfo, sqlConfig, implementationType);
        var h = new RelationalDbHealthContributor((IDbConnection)connFactory.Create(null), factory.CreateLogger<RelationalDbHealthContributor>());

        var status = h.Health();

        Assert.Equal(HealthStatus.Up, status.Status);
    }

    [Fact]
    public void PostgreSql_Not_Connected_Returns_Down_Status()
    {
        var implementationType = PostgreSqlTypeLocator.NpgsqlConnection;
        var sqlConfig = new PostgresProviderConnectorOptions { Timeout = 1 };
        var sInfo = new PostgresServiceInfo("MyId", "postgres://localhost:5432/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355");
        var factory = new LoggerFactory();
        var connFactory = new PostgresProviderConnectorFactory(sInfo, sqlConfig, implementationType);
        var h = new RelationalDbHealthContributor((IDbConnection)connFactory.Create(null), factory.CreateLogger<RelationalDbHealthContributor>());

        var status = h.Health();

        Assert.Equal(HealthStatus.Down, status.Status);
        Assert.Contains(status.Details.Keys, k => k == "error");
    }

    [Fact(Skip = "Integration test - requires local db server")]
    public void PostgreSql_Is_Connected_Returns_Up_Status()
    {
        var implementationType = PostgreSqlTypeLocator.NpgsqlConnection;
        var sqlConfig = new PostgresProviderConnectorOptions();
        var sInfo = new PostgresServiceInfo("MyId", "postgres://steeltoe:steeltoe@localhost:5432/postgres");
        var factory = new LoggerFactory();
        var connFactory = new PostgresProviderConnectorFactory(sInfo, sqlConfig, implementationType);
        var h = new RelationalDbHealthContributor((IDbConnection)connFactory.Create(null), factory.CreateLogger<RelationalDbHealthContributor>());

        var status = h.Health();

        Assert.Equal(HealthStatus.Up, status.Status);
    }

    [Fact]
    public void Oracle_Not_Connected_Returns_Down_Status()
    {
        var implementationType = OracleTypeLocator.OracleConnection;
        var sqlConfig = new OracleProviderConnectorOptions { ConnectionTimeout = 1 };
        var sInfo = new OracleServiceInfo("MyId", "oracle://user:pwd@localhost:1521/someService");
        var factory = new LoggerFactory();
        var connFactory = new OracleProviderConnectorFactory(sInfo, sqlConfig, implementationType);
        var h = new RelationalDbHealthContributor((IDbConnection)connFactory.Create(null), factory.CreateLogger<RelationalDbHealthContributor>());

        var status = h.Health();

        Assert.Equal(HealthStatus.Down, status.Status);
        Assert.Contains(status.Details.Keys, k => k == "error");
    }

    [Fact(Skip = "Integration test - requires local db server")]
    public void Oracle_Is_Connected_Returns_Up_Status()
    {
        var implementationType = OracleTypeLocator.OracleConnection;
        var sqlConfig = new OracleProviderConnectorOptions();
        var sInfo = new OracleServiceInfo("MyId", "oracle://hr:hr@localhost:1521/orclpdb1");
        var factory = new LoggerFactory();
        var connFactory = new OracleProviderConnectorFactory(sInfo, sqlConfig, implementationType);
        var h = new RelationalDbHealthContributor((IDbConnection)connFactory.Create(null), factory.CreateLogger<RelationalDbHealthContributor>());

        var status = h.Health();

        Assert.Equal(HealthStatus.Up, status.Status);
    }
}
