// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Reflection;
using Steeltoe.Connector.MySql;
using Steeltoe.Connector.Oracle;
using Steeltoe.Connector.PostgreSql;
using Steeltoe.Connector.Services;
using Steeltoe.Connector.SqlServer;
using System.Data;
using Steeltoe.Common.Util;

namespace Steeltoe.Connector;

public class RelationalDbHealthContributor : IHealthContributor
{
    public static IHealthContributor GetMySqlContributor(IConfiguration configuration, ILogger<RelationalDbHealthContributor> logger = null)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        var info = configuration.GetSingletonServiceInfo<MySqlServiceInfo>();
        var mySqlConnection = ReflectionHelpers.FindType(MySqlTypeLocator.Assemblies, MySqlTypeLocator.ConnectionTypeNames);
        var mySqlConfig = new MySqlProviderConnectorOptions(configuration);
        var factory = new MySqlProviderConnectorFactory(info, mySqlConfig, mySqlConnection);
        var connection = factory.Create(null) as IDbConnection;
        return new RelationalDbHealthContributor(connection, logger);
    }

    public static IHealthContributor GetPostgreSqlContributor(IConfiguration configuration, ILogger<RelationalDbHealthContributor> logger = null)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        var info = configuration.GetSingletonServiceInfo<PostgresServiceInfo>();
        var postgresConnection = ReflectionHelpers.FindType(PostgreSqlTypeLocator.Assemblies, PostgreSqlTypeLocator.ConnectionTypeNames);
        var postgresConfig = new PostgresProviderConnectorOptions(configuration);
        var factory = new PostgresProviderConnectorFactory(info, postgresConfig, postgresConnection);
        var connection = factory.Create(null) as IDbConnection;
        return new RelationalDbHealthContributor(connection, logger);
    }

    public static IHealthContributor GetSqlServerContributor(IConfiguration configuration, ILogger<RelationalDbHealthContributor> logger = null)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        var info = configuration.GetSingletonServiceInfo<SqlServerServiceInfo>();
        var sqlServerConnection = SqlServerTypeLocator.SqlConnection;
        var sqlServerConfig = new SqlServerProviderConnectorOptions(configuration);
        var factory = new SqlServerProviderConnectorFactory(info, sqlServerConfig, sqlServerConnection);
        var connection = factory.Create(null) as IDbConnection;
        return new RelationalDbHealthContributor(connection, logger);
    }

    public static IHealthContributor GetOracleContributor(IConfiguration configuration, ILogger<RelationalDbHealthContributor> logger = null)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        var info = configuration.GetSingletonServiceInfo<OracleServiceInfo>();
        var oracleConnection = ReflectionHelpers.FindType(OracleTypeLocator.Assemblies, OracleTypeLocator.ConnectionTypeNames);
        var oracleConfig = new OracleProviderConnectorOptions(configuration);
        var factory = new OracleProviderConnectorFactory(info, oracleConfig, oracleConnection);
        var connection = factory.Create(null) as IDbConnection;
        return new RelationalDbHealthContributor(connection, logger);
    }

    public readonly IDbConnection Connection;
    private readonly ILogger<RelationalDbHealthContributor> _logger;

    public RelationalDbHealthContributor(IDbConnection connection, ILogger<RelationalDbHealthContributor> logger = null)
    {
        this.Connection = connection;
        _logger = logger;
        Id = GetDbName(connection);
    }

    public string Id { get; }

    public HealthCheckResult Health()
    {
        _logger?.LogTrace("Checking {DbConnection} health", Id);
        var result = new HealthCheckResult();
        result.Details.Add("database", Id);
        try
        {
            Connection.Open();
            var cmd = Connection.CreateCommand();
            cmd.CommandText = Id.IndexOf("Oracle", StringComparison.OrdinalIgnoreCase) != -1 ? "SELECT 1 FROM dual" : "SELECT 1;";
            cmd.ExecuteScalar();
            result.Details.Add("status", HealthStatus.Up.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
            result.Status = HealthStatus.Up;
            _logger?.LogTrace("{DbConnection} up!", Id);
        }
        catch (Exception e)
        {
            _logger?.LogError("{DbConnection} down! {HealthCheckException}", Id, e.Message);
            result.Details.Add("error", $"{e.GetType().Name}: {e.Message}");
            result.Details.Add("status", HealthStatus.Down.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
            result.Status = HealthStatus.Down;
            result.Description = $"{Id} health check failed";
        }
        finally
        {
            Connection.Close();
        }

        return result;
    }

    private string GetDbName(IDbConnection connection)
    {
        var result = "db";
        switch (connection.GetType().Name)
        {
            case "NpgsqlConnection":
                result = "PostgreSQL";
                break;
            case "SqlConnection":
                result = "SqlServer";
                break;
            case "MySqlConnection":
                result = "MySQL";
                break;
            case "OracleConnection":
                result = "Oracle";
                break;
        }

        return $"{result}-{connection.Database}";
    }
}
