// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.ExceptionServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Extensions;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.RabbitMQ.DynamicTypeAccess;

namespace Steeltoe.Connectors.RabbitMQ;

internal sealed partial class RabbitMQHealthContributor : IHealthContributor, IDisposable
{
    private readonly ILogger<RabbitMQHealthContributor> _logger;
    private readonly ConnectionFactoryInterfaceShim _connectionFactoryInterfaceShim;
    private ConnectionInterfaceShim? _connectionInterfaceShim;

    public string Id => "RabbitMQ";
    public string Host { get; }
    public string? ServiceName { get; set; }

    public RabbitMQHealthContributor(object connectionFactory, string host, ILogger<RabbitMQHealthContributor> logger)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        ArgumentNullException.ThrowIfNull(logger);

        _connectionFactoryInterfaceShim = new ConnectionFactoryInterfaceShim(RabbitMQPackageResolver.Default, connectionFactory);
        Host = host;
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
            cancellationToken.ThrowIfCancellationRequested();

            _connectionInterfaceShim ??= await _connectionFactoryInterfaceShim.CreateConnectionAsync(cancellationToken);

            if (!_connectionInterfaceShim.IsOpen)
            {
                throw new IOException("RabbitMQ connection is closed!");
            }

            if (_connectionInterfaceShim.ServerProperties.TryGetValue("version", out object? version) && version is byte[] versionBytes)
            {
                string versionText = Encoding.UTF8.GetString(versionBytes);
                result.Details.Add("version", versionText);
            }

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

    public void Dispose()
    {
        _connectionInterfaceShim?.Dispose();
        _connectionInterfaceShim = null;
    }

    [LoggerMessage(Level = LogLevel.Trace, Message = "Checking {DbConnection} health at {Host}.")]
    private partial void LogCheckingHealth(string dbConnection, string host);

    [LoggerMessage(Level = LogLevel.Trace, Message = "{DbConnection} at {Host} is up.")]
    private partial void LogHealthUp(string dbConnection, string host);

    [LoggerMessage(Level = LogLevel.Error, Message = "{DbConnection} at {Host} is down.")]
    private partial void LogHealthDown(Exception exception, string dbConnection, string host);
}
