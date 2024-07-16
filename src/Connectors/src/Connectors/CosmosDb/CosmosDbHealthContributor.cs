// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Util;
using Steeltoe.Connectors.CosmosDb.DynamicTypeAccess;
using Steeltoe.Connectors.DynamicTypeAccess;

namespace Steeltoe.Connectors.CosmosDb;

internal sealed class CosmosDbHealthContributor : IHealthContributor, IDisposable
{
    private readonly CosmosClientShimFactory _clientFactory;
    private readonly ILogger<CosmosDbHealthContributor> _logger;
    private CosmosClientShim? _cosmosClientShim;

    public string Id { get; } = "CosmosDB";
    public string Host => _clientFactory.HostName;
    public string ServiceName { get; }

    // This property exists because CosmosClient does not respect any timeouts configured in CosmosClientOptions.
    public TimeSpan Timeout { get; set; } = System.Threading.Timeout.InfiniteTimeSpan;

    public CosmosDbHealthContributor(string serviceName, IServiceProvider serviceProvider, CosmosDbPackageResolver packageResolver,
        ILogger<CosmosDbHealthContributor> logger)
    {
        ArgumentGuard.NotNull(serviceName);
        ArgumentGuard.NotNull(serviceProvider);
        ArgumentGuard.NotNull(packageResolver);
        ArgumentGuard.NotNull(logger);

        ServiceName = serviceName;
        _clientFactory = new CosmosClientShimFactory(serviceName, serviceProvider, packageResolver);
        _logger = logger;
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
            _cosmosClientShim ??= _clientFactory.Create();

            Task task = _cosmosClientShim.ReadAccountAsync();
            await task.WaitAsync(Timeout, cancellationToken);

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

        return result;
    }

    public void Dispose()
    {
        _cosmosClientShim?.Dispose();
        _cosmosClientShim = null;
    }

    private sealed class CosmosClientShimFactory
    {
        private readonly ConnectorShim<CosmosDbOptions> _connectorShim;
        private readonly CosmosDbPackageResolver _packageResolver;

        public string HostName { get; }

        public CosmosClientShimFactory(string serviceName, IServiceProvider serviceProvider, CosmosDbPackageResolver packageResolver)
        {
            ArgumentGuard.NotNull(serviceName);
            ArgumentGuard.NotNull(serviceProvider);
            ArgumentGuard.NotNull(packageResolver);

            ConnectorFactoryShim<CosmosDbOptions> connectorFactoryShim =
                ConnectorFactoryShim<CosmosDbOptions>.FromServiceProvider(serviceProvider, packageResolver.CosmosClientClass.Type);

            _connectorShim = connectorFactoryShim.Get(serviceName);
            _packageResolver = packageResolver;
            HostName = GetHostNameFromConnectionString(_connectorShim.Options.ConnectionString);
        }

        public CosmosClientShim Create()
        {
            object cosmosClient = _connectorShim.GetConnection();
            return new CosmosClientShim(_packageResolver, cosmosClient);
        }

        private static string GetHostNameFromConnectionString(string? connectionString)
        {
            if (connectionString == null)
            {
                return string.Empty;
            }

            var builder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            var uri = new Uri((string)builder["AccountEndpoint"]);
            return uri.Host;
        }
    }
}
