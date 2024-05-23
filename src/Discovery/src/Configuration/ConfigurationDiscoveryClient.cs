// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;

namespace Steeltoe.Discovery.Configuration;

/// <summary>
/// A discovery client that reads service instances from app configuration.
/// </summary>
public sealed class ConfigurationDiscoveryClient : IDiscoveryClient
{
    private readonly IOptionsMonitor<ConfigurationDiscoveryOptions> _optionsMonitor;

    public string Description => "A discovery client that returns service instances from app configuration.";

    public ConfigurationDiscoveryClient(IOptionsMonitor<ConfigurationDiscoveryOptions> optionsMonitor)
    {
        ArgumentGuard.NotNull(optionsMonitor);

        _optionsMonitor = optionsMonitor;
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
        ArgumentGuard.NotNull(serviceId);

        IList<IServiceInstance> instances = _optionsMonitor.CurrentValue.Services.Where(instance =>
            string.Equals(instance.ServiceId, serviceId, StringComparison.OrdinalIgnoreCase)).Cast<IServiceInstance>().ToList();

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
}
