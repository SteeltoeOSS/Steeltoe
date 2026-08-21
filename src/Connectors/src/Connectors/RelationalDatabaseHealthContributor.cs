// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Extensions;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Connectors;

internal sealed partial class RelationalDatabaseHealthContributor : IHealthContributor
{
    private readonly Func<DbConnection> _getConnection;
    private readonly ILogger<RelationalDatabaseHealthContributor> _logger;

    public string Id { get; }
    public string Host { get; }
    public string? ServiceName { get; set; }

    public RelationalDatabaseHealthContributor(Func<DbConnection> getConnection, string databaseType, string? host,
        ILogger<RelationalDatabaseHealthContributor> logger)
    {
        ArgumentNullException.ThrowIfNull(getConnection);
        ArgumentNullException.ThrowIfNull(databaseType);
        ArgumentNullException.ThrowIfNull(logger);

        _getConnection = getConnection;
        Id = databaseType;
        Host = host ?? string.Empty;
        _logger = logger;
    }

    public async Task<HealthCheckResult?> CheckHealthAsync(CancellationToken cancellationToken)
    {
        LogCheckingHealth(Id, Host);

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
            await using DbConnection connection = _getConnection();
            await connection.OpenAsync(cancellationToken);
            DbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            await command.ExecuteScalarAsync(cancellationToken);

            result.Status = HealthStatus.Up;

            LogHealthUp(Id, Host);
        }
        catch (Exception exception)
        {
            exception = exception.UnwrapAll();

            if (exception.IsCancellation())
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }

            LogHealthDown(exception, Id, Host);

            result.Status = HealthStatus.Down;
            result.Description = $"{Id} health check failed";
            result.Details.Add("error", $"{exception.GetType().Name}: {exception.Message}");
        }

        return result;
    }

    [LoggerMessage(Level = LogLevel.Trace, Message = "Checking {DbConnection} health at {Host}.")]
    private partial void LogCheckingHealth(string dbConnection, string host);

    [LoggerMessage(Level = LogLevel.Trace, Message = "{DbConnection} at {Host} is up.")]
    private partial void LogHealthUp(string dbConnection, string host);

    [LoggerMessage(Level = LogLevel.Error, Message = "{DbConnection} at {Host} is down.")]
    private partial void LogHealthDown(Exception exception, string dbConnection, string host);
}
