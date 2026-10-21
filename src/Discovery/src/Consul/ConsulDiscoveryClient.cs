// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Consul.Filtering;
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
public sealed class ConsulDiscoveryClient : IDiscoveryClient, IServiceRegistry
{
    private readonly IConsulClient _client;
    private readonly IOptionsMonitor<ConsulDiscoveryOptions> _optionsMonitor;
    private readonly ConsulServiceRegistrar _registrar;

    /// <inheritdoc />
    public string Description => "A discovery client for HashiCorp Consul.";

    /// <inheritdoc />
    public bool IsRegistrationEnabled => _registrar is { IsRunning: true } && _optionsMonitor.CurrentValue.Register;

    /// <summary>
    /// This event is never raised. The Consul client doesn't implement caching.
    /// </summary>
#pragma warning disable CS0067 // The event is never used
    public event EventHandler<DiscoveryInstancesFetchedEventArgs>? InstancesFetched;
#pragma warning restore CS0067 // The event is never used

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsulDiscoveryClient" /> class.
    /// </summary>
    /// <param name="client">
    /// The Consul client.
    /// </param>
    /// <param name="optionsMonitor">
    /// Provides access to <see cref="ConsulDiscoveryOptions" />.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public ConsulDiscoveryClient(IConsulClient client, IOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        var registrar = new ConsulServiceRegistrar(client, optionsMonitor, loggerFactory);

        _client = client;
        _optionsMonitor = optionsMonitor;
        _registrar = registrar;

#pragma warning disable S4462 // Calls to "async" methods should not be blocking
        // Justification: Async calls from a constructor are not possible. To fix this, an alternate design is needed.
        _registrar.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore S4462 // Calls to "async" methods should not be blocking
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
            ISet<string> serviceIds = await GetServiceIdsAsync(null, null, queryOptions, cancellationToken);

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
        return GetServiceIdsAsync(null, null, QueryOptions.Default, cancellationToken);
    }

    /// <summary>
    /// Gets all registered service IDs from the Consul catalog.
    /// </summary>
    /// <param name="dataCenter">
    /// Specifies the datacenter to query.
    /// </param>
    /// <param name="filter">
    /// Specifies the expression used to filter the queries results prior to returning the data.
    /// </param>
    /// <param name="queryOptions">
    /// Options to parameterize the Consul query.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// The list of service IDs.
    /// </returns>
    public async Task<ISet<string>> GetServiceIdsAsync(string? dataCenter, Filter? filter, QueryOptions queryOptions, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queryOptions);

        ConsulDiscoveryOptions options = _optionsMonitor.CurrentValue;

        if (!options.Enabled)
        {
            return new HashSet<string>();
        }

        QueryResult<Dictionary<string, string[]>> result = await _client.Catalog.Services(dataCenter, filter, queryOptions, cancellationToken);
        return result.Response.Keys.ToHashSet();
    }

    /// <inheritdoc />
    public IServiceInstance GetLocalServiceInstance()
    {
        return _registrar.ServiceInstance;
    }

    /// <inheritdoc />
    public async Task ShutdownAsync(CancellationToken cancellationToken)
    {
        await _registrar.ShutdownAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the status of the local service instance (this app) from Consul.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// "OUT_OF_SERVICE" when the service is in maintenance mode, "UP" otherwise.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The local service instance is not registered with Consul.
    /// </exception>
    public async Task<object> GetStatusAsync(CancellationToken cancellationToken)
    {
        ConsulServiceRegistrar registrar = GetRunningRegistrar();
        return await registrar.Registry.GetStatusAsync(registrar.Registration, cancellationToken);
    }

    /// <summary>
    /// Changes the status of the local service instance (this app) in Consul, by enabling or disabling maintenance mode.
    /// </summary>
    /// <param name="status">
    /// "OUT_OF_SERVICE" to enable maintenance mode, or "UP" to disable it.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <exception cref="ArgumentException">
    /// The specified status is unknown.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The local service instance is not registered with Consul.
    /// </exception>
    public async Task SetStatusAsync(string status, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        ConsulServiceRegistrar registrar = GetRunningRegistrar();
        await registrar.Registry.SetStatusAsync(registrar.Registration, status, cancellationToken);
    }

    private ConsulServiceRegistrar GetRunningRegistrar()
    {
        if (_registrar == null || !IsRegistrationEnabled)
        {
            throw new InvalidOperationException("This app is not registered with Consul.");
        }

        return _registrar;
    }

    internal async Task AddInstancesToListAsync(ICollection<IServiceInstance> instances, string serviceId, QueryOptions queryOptions,
        ConsulDiscoveryOptions options, CancellationToken cancellationToken)
    {
        QueryResult<ServiceEntry[]> result =
            await _client.Health.Service(serviceId, options.DefaultQueryTag, options.QueryPassing, queryOptions, cancellationToken);

        foreach (ConsulServiceInstance instance in result.Response.Select(ConsulServiceInstance.FromServiceEntry))
        {
            instances.Add(instance);
        }
    }
}
