// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;

namespace Steeltoe.Common.Discovery;

public class ConfigurationServiceInstanceProvider : IServiceInstanceProvider
{
    private readonly IOptionsMonitor<List<ConfigurationServiceInstance>> _serviceInstances;

    public string Description => "A service instance provider that returns services from app configuration";

    public ConfigurationServiceInstanceProvider(IOptionsMonitor<List<ConfigurationServiceInstance>> serviceInstances)
    {
        _serviceInstances = serviceInstances;
    }

    public Task<IList<string>> GetServiceIdsAsync(CancellationToken cancellationToken)
    {
        IList<string> services = _serviceInstances.CurrentValue.Select(si => si.ServiceId).Distinct().ToList();
        return Task.FromResult(services);
    }

    public Task<IList<IServiceInstance>> GetInstancesAsync(string serviceId, CancellationToken cancellationToken)
    {
        IList<IServiceInstance> instances = _serviceInstances.CurrentValue.Cast<IServiceInstance>().Where(instance =>
            instance.ServiceId.Equals(serviceId, StringComparison.OrdinalIgnoreCase)).ToList();

        return Task.FromResult(instances);
    }
}
