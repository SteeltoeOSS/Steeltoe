// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Reflection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Util;
using Steeltoe.Connectors.CosmosDb.DynamicTypeAccess;

namespace Steeltoe.Connectors.CosmosDb;

internal sealed class CosmosDbHealthContributor : IHealthContributor, IDisposable
{
    private readonly ILogger<CosmosDbHealthContributor> _logger;
    private readonly CosmosClientShim _cosmosClientShim;

    public string Id { get; } = "CosmosDB";
    public string Host { get; }
    public string? ServiceName { get; set; }

    // This property exists because CosmosClient does not respect any timeouts configured in CosmosClientOptions.
    public TimeSpan Timeout { get; set; } = System.Threading.Timeout.InfiniteTimeSpan;

    public CosmosDbHealthContributor(object cosmosClient, string host, ILogger<CosmosDbHealthContributor> logger)
    {
        ArgumentGuard.NotNull(cosmosClient);
        ArgumentGuard.NotNullOrEmpty(host);
        ArgumentGuard.NotNull(logger);

        _cosmosClientShim = new CosmosClientShim(CosmosDbPackageResolver.Default, cosmosClient);
        Host = host;
        _logger = logger;
    }

    public HealthCheckResult Health()
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
            Task task = _cosmosClientShim.ReadAccountAsync();

            if (!task.Wait(Timeout))
            {
                throw new TimeoutException();
            }

            result.Status = HealthStatus.Up;
            result.Details.Add("status", HealthStatus.Up.ToSnakeCaseString(SnakeCaseStyle.AllCaps));

            _logger.LogTrace("{DbConnection} at {Host} is up!", Id, Host);
        }
        catch (Exception exception)
        {
            if (exception is TargetInvocationException)
            {
                exception = exception.InnerException ?? exception;
            }

            if (exception is AggregateException { InnerExceptions.Count: 1 } aggregateException)
            {
                exception = aggregateException.InnerExceptions[0];
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
        _cosmosClientShim.Dispose();
    }
}
