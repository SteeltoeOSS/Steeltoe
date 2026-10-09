// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Extensions;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.MongoDb.DynamicTypeAccess;

namespace Steeltoe.Connectors.MongoDb;

internal sealed partial class MongoDbHealthContributor : IHealthContributor
{
    private readonly Func<MongoClientInterfaceShim> _getClient;
    private readonly bool _disposeClient;
    private readonly ILogger<MongoDbHealthContributor> _logger;

    public string Id => "MongoDB";
    public string Host { get; }
    public string ServiceName { get; }

    public MongoDbHealthContributor(string serviceName, Func<MongoClientInterfaceShim> getClient, bool disposeClient, string? host,
        ILogger<MongoDbHealthContributor> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceName);
        ArgumentNullException.ThrowIfNull(getClient);
        ArgumentNullException.ThrowIfNull(logger);

        ServiceName = serviceName;
        _getClient = getClient;
        _disposeClient = disposeClient;
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
            MongoClientInterfaceShim mongoClientShim = _getClient();

            // When the connector caches the client, it owns the client and disposes it on shutdown.
            using MongoClientInterfaceShim? clientToDispose = _disposeClient ? mongoClientShim : null;

            using IDisposable cursor = await mongoClientShim.ListDatabaseNamesAsync(cancellationToken);

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
