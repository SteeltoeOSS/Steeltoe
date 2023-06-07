// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Reflection;
using Steeltoe.Common.Util;
using Steeltoe.Connectors.MySql;
using Steeltoe.Connectors.PostgreSql;
using Steeltoe.Connectors.Services;
using Steeltoe.Connectors.SqlServer;

namespace Steeltoe.Connectors;

public class RelationalDbHealthContributor : IHealthContributor
{
    private readonly ILogger<RelationalDbHealthContributor> _logger;

    public readonly DbConnection Connection;

    public string Id { get; }
    public string HostName { get; }

    public RelationalDbHealthContributor(DbConnection connection, ILogger<RelationalDbHealthContributor> logger = null)
    {
        Connection = connection;
        _logger = logger;
        Id = GetDbName(connection);
    }

    internal RelationalDbHealthContributor(DbConnection connection, string serviceName, string hostName, ILogger<RelationalDbHealthContributor> logger = null)
    {
        Id = serviceName;
        HostName = hostName;
        Connection = connection;
        _logger = logger;
    }

    public static IHealthContributor GetMySqlContributor(IConfiguration configuration, ILogger<RelationalDbHealthContributor> logger = null)
    {
        ArgumentGuard.NotNull(configuration);

        var info = configuration.GetSingletonServiceInfo<MySqlServiceInfo>();
        Type mySqlConnection = ReflectionHelpers.FindType(MySqlTypeLocator.Assemblies, MySqlTypeLocator.ConnectionTypeNames);
        var options = new MySqlProviderConnectorOptions(configuration);
        var factory = new MySqlProviderConnectorFactory(info, options, mySqlConnection);
        var connection = factory.Create(null) as DbConnection;
        return new RelationalDbHealthContributor(connection, logger);
    }

    public static IHealthContributor GetPostgreSqlContributor(IConfiguration configuration, ILogger<RelationalDbHealthContributor> logger = null)
    {
        ArgumentGuard.NotNull(configuration);

        var info = configuration.GetSingletonServiceInfo<PostgreSqlServiceInfo>();
        Type postgreSqlConnection = ReflectionHelpers.FindType(PostgreSqlTypeLocator.Assemblies, PostgreSqlTypeLocator.ConnectionTypeNames);
        var options = new PostgreSqlProviderConnectorOptions(configuration);
        var factory = new PostgreSqlProviderConnectorFactory(info, options, postgreSqlConnection);
        var connection = factory.Create(null) as DbConnection;
        return new RelationalDbHealthContributor(connection, logger);
    }

    public static IHealthContributor GetSqlServerContributor(IConfiguration configuration, ILogger<RelationalDbHealthContributor> logger = null)
    {
        ArgumentGuard.NotNull(configuration);

        var info = configuration.GetSingletonServiceInfo<SqlServerServiceInfo>();
        Type sqlServerConnection = SqlServerTypeLocator.SqlConnection;
        var options = new SqlServerProviderConnectorOptions(configuration);
        var factory = new SqlServerProviderConnectorFactory(info, options, sqlServerConnection);
        var connection = factory.Create(null) as DbConnection;
        return new RelationalDbHealthContributor(connection, logger);
    }

    public HealthCheckResult Health()
    {
        _logger?.LogTrace("Checking {DbConnection} health", Id);
        var result = new HealthCheckResult();

        if (!string.IsNullOrEmpty(HostName))
        {
            result.Details.Add("host", HostName);
        }

        try
        {
            Connection.Open();
            DbCommand command = Connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            command.ExecuteScalar();
            result.Details.Add("status", HealthStatus.Up.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
            result.Status = HealthStatus.Up;
            _logger?.LogTrace("{DbConnection} up!", Id);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "{DbConnection} down! {HealthCheckException}", Id, e.Message);
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

    private string GetDbName(DbConnection connection)
    {
        string result = "db";

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
        }

        return $"{result}-{connection.Database}";
    }
}
