// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using HealthStatus = Steeltoe.Common.HealthChecks.HealthStatus;

namespace Steeltoe.Discovery.Consul.Discovery;

/// <summary>
/// A Health contributor which provides the health of the Consul server connection.
/// </summary>
public class ConsulHealthContributor : IHealthContributor
{
    private readonly IConsulClient _client;
    private readonly IOptionsMonitor<ConsulDiscoveryOptions> _optionsMonitor;
    private readonly ConsulDiscoveryOptions _options;

    internal ConsulDiscoveryOptions Options
    {
        get
        {
            if (_optionsMonitor != null)
            {
                return _optionsMonitor.CurrentValue;
            }

            return _options;
        }
    }

    public string Id => "consul";

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsulHealthContributor" /> class.
    /// </summary>
    /// <param name="client">
    /// a Consul client to use for health checks.
    /// </param>
    /// <param name="options">
    /// configuration options.
    /// </param>
    /// <param name="logger">
    /// optional logger.
    /// </param>
    public ConsulHealthContributor(IConsulClient client, ConsulDiscoveryOptions options, ILogger<ConsulHealthContributor> logger = null)
    {
        ArgumentGuard.NotNull(client);
        ArgumentGuard.NotNull(options);

        _client = client;
        _options = options;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsulHealthContributor" /> class.
    /// </summary>
    /// <param name="client">
    /// a Consul client to use for health checks.
    /// </param>
    /// <param name="optionsMonitor">
    /// Provides access to <see cref="ConsulDiscoveryOptions" />.
    /// </param>
    /// <param name="logger">
    /// optional logger.
    /// </param>
    public ConsulHealthContributor(IConsulClient client, IOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor, ILogger<ConsulHealthContributor> logger = null)
    {
        ArgumentGuard.NotNull(client);
        ArgumentGuard.NotNull(optionsMonitor);

        _client = client;
        _optionsMonitor = optionsMonitor;
    }

    /// <summary>
    /// Computes the health of the Consul server connection.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// The health check result.
    /// </returns>
    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken)
    {
        var result = new HealthCheckResult();
        string leaderStatus = await GetLeaderStatusAsync(cancellationToken);
        Dictionary<string, string[]> services = await GetCatalogServicesAsync(cancellationToken);
        result.Status = HealthStatus.Up;
        result.Details.Add("leader", leaderStatus);
        result.Details.Add("services", services);
        return result;
    }

    internal Task<string> GetLeaderStatusAsync(CancellationToken cancellationToken)
    {
        return _client.Status.Leader(cancellationToken);
    }

    internal async Task<Dictionary<string, string[]>> GetCatalogServicesAsync(CancellationToken cancellationToken)
    {
        QueryResult<Dictionary<string, string[]>> result = await _client.Catalog.Services(QueryOptions.Default, cancellationToken);
        return result.Response;
    }
}
