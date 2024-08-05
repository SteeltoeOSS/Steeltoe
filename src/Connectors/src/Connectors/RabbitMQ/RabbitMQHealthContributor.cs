// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.ExceptionServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.CasingConventions;
using Steeltoe.Common.Extensions;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.RabbitMQ.DynamicTypeAccess;

namespace Steeltoe.Connectors.RabbitMQ;

internal sealed class RabbitMQHealthContributor : IHealthContributor, IDisposable
{
    private readonly ILogger<RabbitMQHealthContributor> _logger;
    private readonly ConnectionFactoryInterfaceShim _connectionFactoryInterfaceShim;
    private ConnectionInterfaceShim? _connectionInterfaceShim;

    public string Id { get; } = "RabbitMQ";
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

    public Task<HealthCheckResult?> CheckHealthAsync(CancellationToken cancellationToken)
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
            cancellationToken.ThrowIfCancellationRequested();

            _connectionInterfaceShim ??= _connectionFactoryInterfaceShim.CreateConnection();

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

        return Task.FromResult<HealthCheckResult?>(result);
    }

    public void Dispose()
    {
        _connectionInterfaceShim?.Dispose();
        _connectionInterfaceShim = null;
    }
}
