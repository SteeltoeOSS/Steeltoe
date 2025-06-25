// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.RouteMappings;

internal sealed class ActuatorRouteOptionsResolver
{
    private readonly IOptionsMonitor<ManagementOptions> _managementOptionsMonitor;
    private readonly IEnumerable<IEndpointOptionsMonitorProvider> _endpointOptionsMonitorProviders;

    public ActuatorRouteOptionsResolver(IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        IEnumerable<IEndpointOptionsMonitorProvider> endpointOptionsMonitorProviders)
    {
        ArgumentNullException.ThrowIfNull(managementOptionsMonitor);
        ArgumentNullException.ThrowIfNull(endpointOptionsMonitorProviders);

        _managementOptionsMonitor = managementOptionsMonitor;
        _endpointOptionsMonitorProviders = endpointOptionsMonitorProviders;
    }

    public ActuatorRoutesSnapshot CreateSnapshot()
    {
        return new ActuatorRoutesSnapshot(_managementOptionsMonitor, _endpointOptionsMonitorProviders);
    }
}

internal sealed class ActuatorRoutesSnapshot
{
    private readonly Dictionary<string, EndpointOptions> _endpointOptionsByRoutePattern = new(StringComparer.OrdinalIgnoreCase);

    public ActuatorRoutesSnapshot(IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        IEnumerable<IEndpointOptionsMonitorProvider> endpointOptionsMonitorProviders)
    {
        ManagementOptions managementOptions = managementOptionsMonitor.CurrentValue;

        foreach (EndpointOptions endpointOptions in endpointOptionsMonitorProviders.Select(provider => provider.Get()))
        {
            ConditionalAdd(endpointOptions, managementOptions, managementOptions.Path);
            ConditionalAdd(endpointOptions, managementOptions, ConfigureManagementOptions.DefaultCloudFoundryPath);
        }
    }

    private void ConditionalAdd(EndpointOptions endpointOptions, ManagementOptions managementOptions, string? basePath)
    {
        string routePattern = endpointOptions.GetPathMatchPattern(basePath);

        if (endpointOptions.CanInvoke(routePattern, managementOptions))
        {
            _endpointOptionsByRoutePattern.TryAdd(routePattern, endpointOptions);
        }
    }

    public EndpointOptions? GetOptionsForRoute(string? routePattern)
    {
        return routePattern == null ? null : _endpointOptionsByRoutePattern.GetValueOrDefault(routePattern);
    }
}
