// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Reflection;
using Steeltoe.Common.Util;

namespace Steeltoe.Connector.CosmosDb;

internal sealed class CosmosDbHealthContributor : IHealthContributor
{
    private readonly object _cosmosClient;
    private readonly string _hostName;
    private readonly ILogger<CosmosDbHealthContributor> _logger;
    private readonly int _timeout;

    public string Id { get; }

    internal CosmosDbHealthContributor(object cosmosClient, string serviceName, string hostName, ILogger<CosmosDbHealthContributor> logger, int timeout = 5000)
    {
        ArgumentGuard.NotNull(cosmosClient);
        ArgumentGuard.NotNull(serviceName);
        ArgumentGuard.NotNull(hostName);
        ArgumentGuard.NotNull(logger);

        _cosmosClient = cosmosClient;
        Id = serviceName;
        _hostName = hostName;
        _logger = logger;
        _timeout = timeout;
    }

    public HealthCheckResult Health()
    {
        _logger.LogTrace("Checking CosmosDB connection health");
        var result = new HealthCheckResult();

        if (_hostName != null)
        {
            result.Details.Add("host", _hostName);
        }

        try
        {
            var task = (Task)ReflectionHelpers.Invoke(CosmosDbTypeLocator.ReadAccountAsyncMethod, _cosmosClient, Array.Empty<object>());

            if (!task.Wait(_timeout))
            {
                throw new ConnectorException("Failed to open CosmosDB connection!");
            }

            result.Details.Add("status", HealthStatus.Up.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
            result.Status = HealthStatus.Up;
            _logger.LogTrace("CosmosDB connection is up!");
        }
        catch (Exception exception)
        {
            if (exception is TargetInvocationException)
            {
                exception = exception.InnerException;
            }

            _logger.LogError(exception, "CosmosDB connection is down! {HealthCheckException}", exception.Message);
            result.Details.Add("error", $"{exception.GetType().Name}: {exception.Message}");
            result.Details.Add("status", HealthStatus.Down.ToSnakeCaseString(SnakeCaseStyle.AllCaps));
            result.Status = HealthStatus.Down;
            result.Description = exception.Message;
        }

        return result;
    }
}
