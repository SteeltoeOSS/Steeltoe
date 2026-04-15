// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Discovery;

namespace Steeltoe.Discovery.Configuration;

/// <summary>
/// A discovery client that reads service instances from app configuration.
/// </summary>
public sealed class ConfigurationDiscoveryClient : IDiscoveryClient, IDisposable
{
    private readonly IOptionsMonitor<ConfigurationDiscoveryOptions> _optionsMonitor;
    private readonly IDisposable? _changeTokenRegistration;

    public string Description => "A discovery client that returns service instances from app configuration.";

    /// <summary>
    /// Occurs when the configuration of service instances has been reloaded.
    /// </summary>
    public event EventHandler<DiscoveryInstancesFetchedEventArgs>? InstancesFetched;

    public ConfigurationDiscoveryClient(IOptionsMonitor<ConfigurationDiscoveryOptions> optionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        _optionsMonitor = optionsMonitor;
        _changeTokenRegistration = optionsMonitor.OnChange(OnOptionsChanged);
    }

    private void OnOptionsChanged(ConfigurationDiscoveryOptions options)
    {
        if (InstancesFetched != null)
        {
            ReadOnlyDictionary<string, IReadOnlyList<IServiceInstance>> instancesByServiceId = ToServiceInstanceMap(options.Services);
            var eventArgs = new DiscoveryInstancesFetchedEventArgs(instancesByServiceId);
            RaiseFetchEvent(eventArgs);
        }
    }

    private static ReadOnlyDictionary<string, IReadOnlyList<IServiceInstance>> ToServiceInstanceMap(IList<ConfigurationServiceInstance> services)
    {
        // @formatter:wrap_chained_method_calls chop_always
        // @formatter:wrap_before_first_method_call true

        return services
            .Where(service => service.ServiceId != null)
            .GroupBy(service => service.ServiceId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(grouping => grouping.Key, grouping => (IReadOnlyList<IServiceInstance>)grouping
                .Cast<IServiceInstance>()
                .ToList()
                .AsReadOnly(), StringComparer.OrdinalIgnoreCase)
            .AsReadOnly();

        // @formatter:wrap_before_first_method_call restore
        // @formatter:wrap_chained_method_calls restore
    }

    private void RaiseFetchEvent(DiscoveryInstancesFetchedEventArgs eventArgs)
    {
        // Execute on separate thread, so we won't block the configuration system in case the handler logic is expensive.
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                InstancesFetched?.Invoke(this, eventArgs);
            }
            catch (Exception)
            {
                // Intentionally left empty. Adding a logger to the constructor is a breaking change.
                // Adding an extra constructor confuses the service container.
            }
        });
    }

    /// <inheritdoc />
    public Task<ISet<string>> GetServiceIdsAsync(CancellationToken cancellationToken)
    {
        ISet<string> services = _optionsMonitor.CurrentValue.Services.Select(instance => instance.ServiceId!).Distinct()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Task.FromResult(services);
    }

    /// <inheritdoc />
    public Task<IList<IServiceInstance>> GetInstancesAsync(string serviceId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);

        IList<IServiceInstance> instances = _optionsMonitor.CurrentValue.Services.Where(instance =>
            string.Equals(instance.ServiceId, serviceId, StringComparison.OrdinalIgnoreCase)).Cast<IServiceInstance>().ToArray();

        return Task.FromResult(instances);
    }

    /// <inheritdoc />
    public IServiceInstance? GetLocalServiceInstance()
    {
        return null;
    }

    /// <inheritdoc />
    public Task ShutdownAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _changeTokenRegistration?.Dispose();
    }
}
