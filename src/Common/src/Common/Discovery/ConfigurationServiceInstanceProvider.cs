// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;

namespace Steeltoe.Common.Discovery;

public class ConfigurationServiceInstanceProvider : IServiceInstanceProvider
{
    private readonly IOptionsMonitor<List<ConfigurationServiceInstance>> _serviceInstances;

    public string Description => "A service instance provider that returns services from app configuration";

    public IList<string> Services => GetServices();

    public ConfigurationServiceInstanceProvider(IOptionsMonitor<List<ConfigurationServiceInstance>> serviceInstances)
    {
        _serviceInstances = serviceInstances;
    }

    public IList<IServiceInstance> GetInstances(string serviceId)
    {
        return new List<IServiceInstance>(_serviceInstances.CurrentValue.Where(si => si.ServiceId.Equals(serviceId, StringComparison.OrdinalIgnoreCase)));
    }

    internal IList<string> GetServices()
    {
        return _serviceInstances.CurrentValue.Select(si => si.ServiceId).Distinct().ToList();
    }
}
