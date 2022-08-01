// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;
using HealthStatus = Steeltoe.Common.HealthChecks.HealthStatus;

namespace Steeltoe.Discovery.Consul.Discovery;

/// <summary>
/// A Health contributor which provides the health of the Consul server connection.
/// </summary>
public class ConsulHealthContributor : IHealthContributor
{
    private readonly IConsulClient _client;
    private readonly ILogger<ConsulHealthContributor> _logger;
    private readonly IOptionsMonitor<ConsulDiscoveryOptions> _optionsMonitor;
    private readonly ConsulDiscoveryOptions _options;

    public string Id => "consul";

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

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsulHealthContributor"/> class.
    /// </summary>
    /// <param name="client">a Consul client to use for health checks.</param>
    /// <param name="options">configuration options.</param>
    /// <param name="logger">optional logger.</param>
    public ConsulHealthContributor(IConsulClient client, ConsulDiscoveryOptions options, ILogger<ConsulHealthContributor> logger = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsulHealthContributor"/> class.
    /// </summary>
    /// <param name="client">a Consul client to use for health checks.</param>
    /// <param name="optionsMonitor">configuration options.</param>
    /// <param name="logger">optional logger.</param>
    public ConsulHealthContributor(IConsulClient client, IOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor, ILogger<ConsulHealthContributor> logger = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger;
    }

    /// <summary>
    /// Compute the health of the Consul server connection.
    /// </summary>
    /// <returns>the health check result.</returns>
    public HealthCheckResult Health()
    {
        var result = new HealthCheckResult();
        var leaderStatus = GetLeaderStatusAsync().GetAwaiter().GetResult();
        var services = GetCatalogServicesAsync().GetAwaiter().GetResult();
        result.Status = HealthStatus.Up;
        result.Details.Add("leader", leaderStatus);
        result.Details.Add("services", services);
        return result;
    }

    internal Task<string> GetLeaderStatusAsync()
    {
        return _client.Status.Leader();
    }

    internal async Task<Dictionary<string, string[]>> GetCatalogServicesAsync()
    {
        var result = await _client.Catalog.Services(QueryOptions.Default).ConfigureAwait(false);
        return result.Response;
    }
}
