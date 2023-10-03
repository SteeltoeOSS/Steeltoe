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
public class ConsulDiscoveryClient : IConsulDiscoveryClient
{
    private readonly IConsulClient _client;
    private readonly IOptionsMonitor<ConsulDiscoveryOptions> _optionsMonitor;
    private readonly ConsulDiscoveryOptions _options;
    private readonly IServiceInstance _thisServiceInstance;
    private readonly IConsulServiceRegistrar _registrar;

    private ConsulDiscoveryOptions Options => _optionsMonitor != null ? _optionsMonitor.CurrentValue : _options;

    /// <inheritdoc />
    public string Description { get; } = "HashiCorp Consul Client";

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
    public ConsulDiscoveryClient(IConsulClient client, ConsulDiscoveryOptions options, IConsulServiceRegistrar registrar = null,
        ILogger<ConsulDiscoveryClient> logger = null)
    {
        ArgumentGuard.NotNull(client);
        ArgumentGuard.NotNull(options);

        _client = client;
        _options = options;
        _registrar = registrar;

        if (_registrar != null)
        {
            _registrar.Start();
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
    /// the configuration options.
    /// </param>
    /// <param name="registrar">
    /// a Consul registrar service.
    /// </param>
    /// <param name="logger">
    /// optional logger.
    /// </param>
    public ConsulDiscoveryClient(IConsulClient client, IOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor, IConsulServiceRegistrar registrar = null,
        ILogger<ConsulDiscoveryClient> logger = null)
    {
        ArgumentGuard.NotNull(client);
        ArgumentGuard.NotNull(optionsMonitor);

        _client = client;
        _optionsMonitor = optionsMonitor;
        _registrar = registrar;

        if (_registrar != null)
        {
            _registrar.Start();
            _thisServiceInstance = new ThisServiceInstance(_registrar.Registration);
        }
    }

    /// <inheritdoc />
    public Task<IList<IServiceInstance>> GetInstancesAsync(string serviceId, CancellationToken cancellationToken)
    {
        return GetInstancesAsync(serviceId, QueryOptions.Default, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IList<IServiceInstance>> GetInstancesAsync(string serviceId, QueryOptions queryOptions, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(serviceId);
        ArgumentGuard.NotNull(queryOptions);

        var instances = new List<IServiceInstance>();
        await AddInstancesToListAsync(instances, serviceId, queryOptions, cancellationToken);
        return instances;
    }

    /// <inheritdoc />
    public async Task<IList<IServiceInstance>> GetAllInstancesAsync(QueryOptions queryOptions, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(queryOptions);

        var instances = new List<IServiceInstance>();
        IList<string> serviceIds = await GetServiceNamesAsync(queryOptions, cancellationToken);

        foreach (string serviceId in serviceIds)
        {
            await AddInstancesToListAsync(instances, serviceId, queryOptions, cancellationToken);
        }

        return instances;
    }

    /// <inheritdoc />
    public async Task<IList<string>> GetServiceNamesAsync(QueryOptions queryOptions, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(queryOptions);

        QueryResult<Dictionary<string, string[]>> result = await _client.Catalog.Services(queryOptions, cancellationToken);
        Dictionary<string, string[]> response = result.Response;
        return response.Keys.ToList();
    }

    /// <inheritdoc />
    public Task<IList<string>> GetServicesAsync(CancellationToken cancellationToken)
    {
        return GetServiceNamesAsync(QueryOptions.Default, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IServiceInstance> GetLocalServiceInstanceAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_thisServiceInstance);
    }

    /// <inheritdoc />
    public Task ShutdownAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
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

    /// <summary>
    /// Dispose of the client and also the Consul service registrar if provided.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _registrar?.Dispose();
        }
    }
}
