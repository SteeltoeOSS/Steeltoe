// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.CasingConventions;
using Steeltoe.Common.Extensions;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.DynamicTypeAccess;
using Steeltoe.Connectors.MongoDb.DynamicTypeAccess;

namespace Steeltoe.Connectors.MongoDb;

internal sealed class MongoDbHealthContributor : IHealthContributor
{
    private readonly MongoClientInterfaceShimFactory _clientFactory;
    private readonly ILogger<MongoDbHealthContributor> _logger;
    private MongoClientInterfaceShim? _mongoClientShim;

    public string Id { get; } = "MongoDB";
    public string Host => _clientFactory.HostName;
    public string ServiceName { get; }

    public MongoDbHealthContributor(string serviceName, IServiceProvider serviceProvider, MongoDbPackageResolver packageResolver,
        ILogger<MongoDbHealthContributor> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceName);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(packageResolver);
        ArgumentNullException.ThrowIfNull(logger);

        ServiceName = serviceName;
        _clientFactory = new MongoClientInterfaceShimFactory(serviceName, serviceProvider, packageResolver);
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
            _mongoClientShim ??= _clientFactory.Create();

            using IDisposable cursor = await _mongoClientShim.ListDatabaseNamesAsync(cancellationToken);

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

    private sealed class MongoClientInterfaceShimFactory
    {
        private readonly ConnectorShim<MongoDbOptions> _connectorShim;

        public string HostName { get; }

        public MongoClientInterfaceShimFactory(string serviceName, IServiceProvider serviceProvider, MongoDbPackageResolver packageResolver)
        {
            ArgumentNullException.ThrowIfNull(serviceName);
            ArgumentNullException.ThrowIfNull(serviceProvider);
            ArgumentNullException.ThrowIfNull(packageResolver);

            ConnectorFactoryShim<MongoDbOptions> connectorFactoryShim =
                ConnectorFactoryShim<MongoDbOptions>.FromServiceProvider(serviceProvider, packageResolver.MongoClientInterface.Type);

            _connectorShim = connectorFactoryShim.Get(serviceName);
            HostName = GetHostNameFromConnectionString(_connectorShim.Options.ConnectionString);
        }

        public MongoClientInterfaceShim Create()
        {
            object mongoClient = _connectorShim.GetConnection();
            return new MongoClientInterfaceShim(MongoDbPackageResolver.Default, mongoClient);
        }

        private static string GetHostNameFromConnectionString(string? connectionString)
        {
            if (connectionString == null)
            {
                return string.Empty;
            }

            var builder = new MongoDbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            string? hostName = (string?)builder["server"];
            return hostName ?? string.Empty;
        }
    }
}
