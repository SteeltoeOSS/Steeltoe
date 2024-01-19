// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Consul.Registry;

namespace Steeltoe.Discovery.Consul.Discovery;

/// <summary>
/// A discovery client for
/// <see href="https://www.consul.io/">
/// HashiCorp Consul
/// </see>
/// .
/// </summary>
public sealed class ConsulDiscoveryClient : IDiscoveryClient, IAsyncDisposable
{
    private readonly IConsulClient _client;
    private readonly IOptionsMonitor<ConsulDiscoveryOptions> _optionsMonitor;
    private readonly ConsulDiscoveryOptions _options;
    private readonly IServiceInstance _thisServiceInstance;
    private readonly ConsulServiceRegistrar _registrar;

    private ConsulDiscoveryOptions Options => _optionsMonitor != null ? _optionsMonitor.CurrentValue : _options;

    /// <inheritdoc />
    public string Description { get; } = "A discovery client for HashiCorp Consul.";

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsulDiscoveryClient" /> class.
    /// </summary>
    /// <param name="client">
    /// a Consul client.
    /// </param>
    /// <param name="options">
    /// the configuration options.
    /// </param>
    /// <param name="registrar">
    /// a Consul registrar service.
    /// </param>
    /// <param name="logger">
    /// optional logger.
    /// </param>
    public ConsulDiscoveryClient(IConsulClient client, ConsulDiscoveryOptions options, ConsulServiceRegistrar registrar = null,
        ILogger<ConsulDiscoveryClient> logger = null)
    {
        ArgumentGuard.NotNull(client);
        ArgumentGuard.NotNull(options);

        _client = client;
        _options = options;
        _registrar = registrar;

        if (_registrar != null)
        {
            _registrar.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
            _thisServiceInstance = new ThisServiceInstance(_registrar.Registration);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsulDiscoveryClient" /> class.
    /// </summary>
    /// <param name="client">
    /// a Consul client.
    /// </param>
    /// <param name="optionsMonitor">
    /// Provides access to <see cref="ConsulDiscoveryOptions" />.
    /// </param>
    /// <param name="registrar">
    /// a Consul registrar service.
    /// </param>
    /// <param name="logger">
    /// optional logger.
    /// </param>
    public ConsulDiscoveryClient(IConsulClient client, IOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor, ConsulServiceRegistrar registrar = null,
        ILogger<ConsulDiscoveryClient> logger = null)
    {
        ArgumentGuard.NotNull(client);
        ArgumentGuard.NotNull(optionsMonitor);

        _client = client;
        _optionsMonitor = optionsMonitor;
        _registrar = registrar;

        if (_registrar != null)
        {
            _registrar.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
            _thisServiceInstance = new ThisServiceInstance(_registrar.Registration);
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
        ArgumentGuard.NotNull(serviceId);
        ArgumentGuard.NotNull(queryOptions);

        var instances = new List<IServiceInstance>();
        await AddInstancesToListAsync(instances, serviceId, queryOptions, cancellationToken);
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
        ArgumentGuard.NotNull(queryOptions);

        var instances = new List<IServiceInstance>();
        IList<string> serviceIds = await GetServiceIdsAsync(queryOptions, cancellationToken);

        foreach (string serviceId in serviceIds)
        {
            await AddInstancesToListAsync(instances, serviceId, queryOptions, cancellationToken);
        }

        return instances;
    }

    /// <inheritdoc />
    public Task<IList<string>> GetServiceIdsAsync(CancellationToken cancellationToken)
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
    public async Task<IList<string>> GetServiceIdsAsync(QueryOptions queryOptions, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(queryOptions);

        QueryResult<Dictionary<string, string[]>> result = await _client.Catalog.Services(queryOptions, cancellationToken);
        return result.Response.Keys.ToList();
    }

    /// <inheritdoc />
    public IServiceInstance GetLocalServiceInstance()
    {
        return _thisServiceInstance;
    }

    /// <inheritdoc />
    public async Task ShutdownAsync(CancellationToken cancellationToken)
    {
        if (_registrar != null)
        {
            await _registrar.DisposeAsync();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await ShutdownAsync(CancellationToken.None);
    }

    internal async Task AddInstancesToListAsync(ICollection<IServiceInstance> instances, string serviceId, QueryOptions queryOptions,
        CancellationToken cancellationToken)
    {
        QueryResult<ServiceEntry[]> result =
            await _client.Health.Service(serviceId, Options.DefaultQueryTag, Options.QueryPassing, queryOptions, cancellationToken);

        foreach (ConsulServiceInstance instance in result.Response.Select(entry => new ConsulServiceInstance(entry)))
        {
            instances.Add(instance);
        }
    }
}
