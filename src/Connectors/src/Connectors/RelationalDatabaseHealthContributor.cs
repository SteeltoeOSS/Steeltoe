// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Util;

namespace Steeltoe.Connectors;

internal sealed class RelationalDatabaseHealthContributor : IHealthContributor, IDisposable
{
    private readonly DbConnection _connection;
    private readonly ILogger<RelationalDatabaseHealthContributor> _logger;

    public string Id { get; }
    public string Host { get; }
    public string? ServiceName { get; set; }

    public RelationalDatabaseHealthContributor(DbConnection connection, string host, ILogger<RelationalDatabaseHealthContributor> logger)
    {
        ArgumentGuard.NotNull(connection);
        ArgumentGuard.NotNullOrEmpty(host);
        ArgumentGuard.NotNull(logger);

        _connection = connection;
        Host = host;
        _logger = logger;
        Id = GetDatabaseType(connection);
    }

    public async Task<HealthCheckResult?> CheckHealthAsync(CancellationToken cancellationToken)
    {
        _logger.LogTrace("Checking {DbConnection} health at {Host}", Id, Host);

        var result = new HealthCheckResult
        {
            Details =
            {
                ["host"] = Host
            }
        };

        if (!string.IsNullOrEmpty(ServiceName))
        {
            result.Details["service"] = ServiceName;
        }

        try
        {
            await _connection.OpenAsync(cancellationToken);
            DbCommand command = _connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            await command.ExecuteScalarAsync(cancellationToken);

            result.Status = HealthStatus.Up;
            result.Details.Add("status", HealthStatus.Up.ToSnakeCaseString(SnakeCaseStyle.AllCaps));

            _logger.LogTrace("{DbConnection} at {Host} is up!", Id, Host);
        }
        catch (Exception exception)
        {
            exception = exception.UnwrapAll();

            if (exception.IsCancellation())
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }

            _logger.LogError(exception, "{DbConnection} at {Host} is down!", Id, Host);

            result.Status = HealthStatus.Down;
            result.Description = $"{Id} health check failed";
            result.Details.Add("error", $"{exception.GetType().Name}: {exception.Message}");
            result.Details.Add("status", HealthStatus.Down.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
        }
        finally
        {
            await _connection.CloseAsync();
        }

        return result;
    }

    private static string GetDatabaseType(DbConnection connection)
    {
        return connection.GetType().Name switch
        {
            "NpgsqlConnection" => "PostgreSQL",
            "SqlConnection" => "SQL Server",
            "MySqlConnection" => "MySQL",
            _ => "unknown"
        };
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
