// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.CasingConventions;
using Steeltoe.Common.Extensions;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.Redis.DynamicTypeAccess;

namespace Steeltoe.Connectors.Redis;

internal sealed class RedisHealthContributor : IHealthContributor, IDisposable
{
    private readonly ILogger<RedisHealthContributor> _logger;
    private readonly string? _connectionString;

    private ConnectionMultiplexerInterfaceShim? _connectionMultiplexerInterfaceShim;
    private DatabaseInterfaceShim? _databaseInterfaceShim;

    public string Id { get; } = "Redis";
    public string Host { get; }
    public string? ServiceName { get; set; }

    public RedisHealthContributor(string? connectionString, ILogger<RedisHealthContributor> logger)
    {
        ArgumentGuard.NotNull(logger);

        _connectionString = connectionString;
        Host = GetHostNameFromConnectionString(connectionString);
        _logger = logger;
    }

    private static string GetHostNameFromConnectionString(string? connectionString)
    {
        if (connectionString == null)
        {
            return string.Empty;
        }

        var builder = new RedisConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        return (string)builder["host"]!;
    }

    internal void SetConnectionMultiplexer(object connectionMultiplexer)
    {
        _connectionMultiplexerInterfaceShim = new ConnectionMultiplexerInterfaceShim(StackExchangeRedisPackageResolver.Default, connectionMultiplexer);
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
            cancellationToken.ThrowIfCancellationRequested();

            _connectionMultiplexerInterfaceShim ??= await ConnectionMultiplexerShim.ConnectAsync(StackExchangeRedisPackageResolver.Default, _connectionString);
            _databaseInterfaceShim ??= _connectionMultiplexerInterfaceShim.GetDatabase();
            TimeSpan roundTripTime = await _databaseInterfaceShim.PingAsync();

            result.Status = HealthStatus.Up;
            result.Details.Add("status", HealthStatus.Up.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
            result.Details.Add("ping", roundTripTime.TotalMilliseconds);

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

        return result;
    }

    public void Dispose()
    {
        _connectionMultiplexerInterfaceShim?.Dispose();
        _connectionMultiplexerInterfaceShim = null;
    }
}
