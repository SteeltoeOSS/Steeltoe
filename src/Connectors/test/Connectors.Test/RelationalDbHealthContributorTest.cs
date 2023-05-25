// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.MySql;
using Steeltoe.Connectors.PostgreSql;
using Steeltoe.Connectors.Services;
using Steeltoe.Connectors.SqlServer;
using Xunit;

namespace Steeltoe.Connectors.Test;

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
        IConfigurationRoot configurationRoot = configurationBuilder.Build();
        IHealthContributor contrib = RelationalDbHealthContributor.GetMySqlContributor(configurationRoot);
        Assert.NotNull(contrib);
        HealthCheckResult status = contrib.Health();
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
        IConfigurationRoot configurationRoot = configurationBuilder.Build();
        IHealthContributor contrib = RelationalDbHealthContributor.GetPostgreSqlContributor(configurationRoot);
        Assert.NotNull(contrib);
        HealthCheckResult status = contrib.Health();
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
        IConfigurationRoot configurationRoot = configurationBuilder.Build();
        IHealthContributor contrib = RelationalDbHealthContributor.GetSqlServerContributor(configurationRoot);
        Assert.NotNull(contrib);
        HealthCheckResult status = contrib.Health();
        Assert.Equal(HealthStatus.Down, status.Status);
    }

    [Fact]
    public void Sql_Not_Connected_Returns_Down_Status()
    {
        Type implementationType = SqlServerTypeLocator.SqlConnection;

        var options = new SqlServerProviderConnectorOptions
        {
            Timeout = 1
        };

        var sInfo = new SqlServerServiceInfo("MyId", "jdbc:sqlserver://localhost:1433/databaseName=invalidDatabaseName", "Dd6O1BPXUHdrmzbP",
            "7E1LxXnlH2hhlPVt");

        var factory = new LoggerFactory();
        var connFactory = new SqlServerProviderConnectorFactory(sInfo, options, implementationType);
        var h = new RelationalDbHealthContributor((DbConnection)connFactory.Create(null), factory.CreateLogger<RelationalDbHealthContributor>());

        HealthCheckResult status = h.Health();

        Assert.Equal(HealthStatus.Down, status.Status);
        Assert.Contains(status.Details.Keys, k => k == "error");
    }

    [Fact(Skip = "Integration test - requires local db server")]
    public void Sql_Is_Connected_Returns_Up_Status()
    {
        Type implementationType = SqlServerTypeLocator.SqlConnection;

        var options = new SqlServerProviderConnectorOptions
        {
            Timeout = 1,
            ConnectionString = "Server=(localdb)\\MSSQLLocalDB;Integrated Security=true"
        };

        var sInfo = new SqlServerServiceInfo("MyId", string.Empty);
        var factory = new LoggerFactory();
        var connFactory = new SqlServerProviderConnectorFactory(sInfo, options, implementationType);
        var h = new RelationalDbHealthContributor((DbConnection)connFactory.Create(null), factory.CreateLogger<RelationalDbHealthContributor>());

        HealthCheckResult status = h.Health();

        Assert.Equal(HealthStatus.Up, status.Status);
    }

    [Fact]
    public void MySql_Not_Connected_Returns_Down_Status()
    {
        Type implementationType = MySqlTypeLocator.MySqlConnection;

        var options = new MySqlProviderConnectorOptions
        {
            ConnectionTimeout = 1
        };

        var sInfo = new MySqlServiceInfo("MyId", "mysql://localhost:80;databaseName=invalidDatabaseName");
        var factory = new LoggerFactory();
        var connFactory = new MySqlProviderConnectorFactory(sInfo, options, implementationType);
        var h = new RelationalDbHealthContributor((DbConnection)connFactory.Create(null), factory.CreateLogger<RelationalDbHealthContributor>());

        HealthCheckResult status = h.Health();

        Assert.Equal(HealthStatus.Down, status.Status);
        Assert.Contains(status.Details.Keys, k => k == "error");
    }

    [Fact(Skip = "Integration test - requires local db server")]
    public void MySql_Is_Connected_Returns_Up_Status()
    {
        Type implementationType = MySqlTypeLocator.MySqlConnection;

        var options = new MySqlProviderConnectorOptions
        {
            ConnectionTimeout = 1
        };

        var sInfo = new MySqlServiceInfo("MyId", "mysql://steeltoe:steeltoe@localhost:3306");
        var factory = new LoggerFactory();
        var connFactory = new MySqlProviderConnectorFactory(sInfo, options, implementationType);
        var h = new RelationalDbHealthContributor((DbConnection)connFactory.Create(null), factory.CreateLogger<RelationalDbHealthContributor>());

        HealthCheckResult status = h.Health();

        Assert.Equal(HealthStatus.Up, status.Status);
    }

    [Fact]
    public void PostgreSql_Not_Connected_Returns_Down_Status()
    {
        Type implementationType = PostgreSqlTypeLocator.NpgsqlConnection;

        var options = new PostgreSqlProviderConnectorOptions
        {
            Timeout = 1
        };

        var sInfo = new PostgreSqlServiceInfo("MyId", "postgres://localhost:5432/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355");
        var factory = new LoggerFactory();
        var connFactory = new PostgreSqlProviderConnectorFactory(sInfo, options, implementationType);
        var h = new RelationalDbHealthContributor((DbConnection)connFactory.Create(null), factory.CreateLogger<RelationalDbHealthContributor>());

        HealthCheckResult status = h.Health();

        Assert.Equal(HealthStatus.Down, status.Status);
        Assert.Contains(status.Details.Keys, k => k == "error");
    }

    [Fact(Skip = "Integration test - requires local db server")]
    public void PostgreSql_Is_Connected_Returns_Up_Status()
    {
        Type implementationType = PostgreSqlTypeLocator.NpgsqlConnection;
        var options = new PostgreSqlProviderConnectorOptions();
        var sInfo = new PostgreSqlServiceInfo("MyId", "postgres://steeltoe:steeltoe@localhost:5432/postgres");
        var factory = new LoggerFactory();
        var connFactory = new PostgreSqlProviderConnectorFactory(sInfo, options, implementationType);
        var h = new RelationalDbHealthContributor((DbConnection)connFactory.Create(null), factory.CreateLogger<RelationalDbHealthContributor>());

        HealthCheckResult status = h.Health();

        Assert.Equal(HealthStatus.Up, status.Status);
    }
}
