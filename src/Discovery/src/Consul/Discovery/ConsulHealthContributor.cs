// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Consul;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using HealthStatus = Steeltoe.Common.HealthChecks.HealthStatus;

namespace Steeltoe.Discovery.Consul.Discovery;

/// <summary>
/// A health contributor that provides the health of the Consul server connection.
/// </summary>
public sealed class ConsulHealthContributor : IHealthContributor
{
    private readonly IConsulClient _client;

    public string Id => "consul";

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsulHealthContributor" /> class.
    /// </summary>
    /// <param name="client">
    /// The Consul client to use for health checks.
    /// </param>
    public ConsulHealthContributor(IConsulClient client)
    {
        ArgumentGuard.NotNull(client);

        _client = client;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult?> CheckHealthAsync(CancellationToken cancellationToken)
    {
        string leaderStatus = await GetLeaderStatusAsync(cancellationToken);
        Dictionary<string, string[]> services = await GetCatalogServicesAsync(cancellationToken);

        return new HealthCheckResult
        {
            Status = HealthStatus.Up,
            Details =
            {
                ["leader"] = leaderStatus,
                ["services"] = services
            }
        };
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
