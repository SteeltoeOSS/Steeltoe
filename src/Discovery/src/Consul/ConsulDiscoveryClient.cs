// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Consul.Configuration;
using Steeltoe.Discovery.Consul.Registry;

namespace Steeltoe.Discovery.Consul;

/// <summary>
/// A discovery client for
/// <see href="https://www.consul.io/">
/// HashiCorp Consul
/// </see>
/// .
/// </summary>
public sealed class ConsulDiscoveryClient : IDiscoveryClient
{
    private readonly IConsulClient _client;
    private readonly IOptionsMonitor<ConsulDiscoveryOptions> _optionsMonitor;
    private readonly ConsulServiceRegistrar? _registrar;
    private readonly ThisServiceInstance? _thisServiceInstance;

    /// <inheritdoc />
    public string Description => "A discovery client for HashiCorp Consul.";

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsulDiscoveryClient" /> class.
    /// </summary>
    /// <param name="client">
    /// The Consul client.
    /// </param>
    /// <param name="optionsMonitor">
    /// Provides access to <see cref="ConsulDiscoveryOptions" />.
    /// </param>
    /// <param name="registrar">
    /// The Consul registrar service.
    /// </param>
    /// <param name="logger">
    /// Used for internal logging. Pass <see cref="NullLogger{T}.Instance" /> to disable logging.
    /// </param>
    public ConsulDiscoveryClient(IConsulClient client, IOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor, ConsulServiceRegistrar? registrar,
        ILogger<ConsulDiscoveryClient> logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(logger);

        _client = client;
        _optionsMonitor = optionsMonitor;
        _registrar = registrar;

        if (registrar != null)
        {
            _thisServiceInstance = new ThisServiceInstance(registrar.Registration);
            registrar.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
    }

    /// <inheritdoc />
    public Task<IList<IServiceInstance>> GetInstancesAsync(string serviceId, CancellationToken cancellationToken)
    {
        return GetInstancesAsync(serviceId, QueryOptions.Default, cancellationToken);
    }

    /// <summary>
    /// Gets all service instances associated with the specified service ID from the Consul catalog.
    /// </summary>
    /// <param name="serviceId">
    /// The ID of the service to lookup.
    /// </param>
    /// <param name="queryOptions">
    /// Any Consul query options to use.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// The list of remote service instances.
    /// </returns>
    public async Task<IList<IServiceInstance>> GetInstancesAsync(string serviceId, QueryOptions queryOptions, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);
        ArgumentNullException.ThrowIfNull(queryOptions);

        List<IServiceInstance> instances = [];
        ConsulDiscoveryOptions options = _optionsMonitor.CurrentValue;

        if (options.Enabled)
        {
            await AddInstancesToListAsync(instances, serviceId, queryOptions, options, cancellationToken);
        }

        return instances;
    }

    /// <summary>
    /// Gets all service instances from the Consul catalog.
    /// </summary>
    /// <param name="queryOptions">
    /// Any Consul query options to use.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// The list of remote service instances.
    /// </returns>
    public async Task<IList<IServiceInstance>> GetAllInstancesAsync(QueryOptions queryOptions, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queryOptions);

        List<IServiceInstance> instances = [];
        ConsulDiscoveryOptions options = _optionsMonitor.CurrentValue;

        if (options.Enabled)
        {
            ISet<string> serviceIds = await GetServiceIdsAsync(queryOptions, cancellationToken);

            foreach (string serviceId in serviceIds)
            {
                await AddInstancesToListAsync(instances, serviceId, queryOptions, options, cancellationToken);
            }
        }

        return instances;
    }

    /// <inheritdoc />
    public Task<ISet<string>> GetServiceIdsAsync(CancellationToken cancellationToken)
    {
        return GetServiceIdsAsync(QueryOptions.Default, cancellationToken);
    }

    /// <summary>
    /// Gets all registered service IDs from the Consul catalog.
    /// </summary>
    /// <param name="queryOptions">
    /// Any Consul query options to use.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// The list of service IDs.
    /// </returns>
    public async Task<ISet<string>> GetServiceIdsAsync(QueryOptions queryOptions, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queryOptions);

        ConsulDiscoveryOptions options = _optionsMonitor.CurrentValue;

        if (!options.Enabled)
        {
            return new HashSet<string>();
        }

        QueryResult<Dictionary<string, string[]>> result = await _client.Catalog.Services(queryOptions, cancellationToken);
        return result.Response.Keys.ToHashSet();
    }

    /// <inheritdoc />
    public IServiceInstance? GetLocalServiceInstance()
    {
        return _thisServiceInstance;
    }

    /// <inheritdoc />
    public async Task ShutdownAsync(CancellationToken cancellationToken)
    {
        if (_registrar != null)
        {
            ConsulDiscoveryOptions options = _optionsMonitor.CurrentValue;

            if (options.Enabled)
            {
                await _registrar.DisposeAsync();
            }
        }
    }

    internal async Task AddInstancesToListAsync(ICollection<IServiceInstance> instances, string serviceId, QueryOptions queryOptions,
        ConsulDiscoveryOptions options, CancellationToken cancellationToken)
    {
        QueryResult<ServiceEntry[]> result =
            await _client.Health.Service(serviceId, options.DefaultQueryTag, options.QueryPassing, queryOptions, cancellationToken);

        foreach (ConsulServiceInstance instance in result.Response.Select(entry => new ConsulServiceInstance(entry)))
        {
            instances.Add(instance);
        }
    }
}
