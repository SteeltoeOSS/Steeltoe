// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.RouteMappingsNew.ResponseTypes;
using Steeltoe.Management.Endpoint.Actuators.RouteMappingsNew.RoutingTypes;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.RouteMappingsNew;

internal sealed class RouteMappingsEndpointHandler : IRouteMappingsEndpointHandler
{
    private readonly IOptionsMonitor<ManagementOptions> _managementOptionsMonitor;
    private readonly IOptionsMonitor<RouteMappingsEndpointOptions> _endpointOptionsMonitor;
    private readonly IEnumerable<IEndpointOptionsMonitorProvider> _endpointOptionsMonitorProviders;
    private readonly AspNetEndpointProvider _aspNetEndpointProvider;

    public EndpointOptions Options => _endpointOptionsMonitor.CurrentValue;

    public RouteMappingsEndpointHandler(IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        IOptionsMonitor<RouteMappingsEndpointOptions> endpointOptionsMonitor, IEnumerable<IEndpointOptionsMonitorProvider> endpointOptionsMonitorProviders,
        AspNetEndpointProvider aspNetEndpointProvider)
    {
        ArgumentNullException.ThrowIfNull(managementOptionsMonitor);
        ArgumentNullException.ThrowIfNull(endpointOptionsMonitor);
        ArgumentNullException.ThrowIfNull(endpointOptionsMonitorProviders);
        ArgumentNullException.ThrowIfNull(aspNetEndpointProvider);

        _managementOptionsMonitor = managementOptionsMonitor;
        _endpointOptionsMonitor = endpointOptionsMonitor;
        _endpointOptionsMonitorProviders = endpointOptionsMonitorProviders;
        _aspNetEndpointProvider = aspNetEndpointProvider;
    }

    public Task<RouteMappingsResponse> InvokeAsync(object? argument, CancellationToken cancellationToken)
    {
        IEnumerable<AspNetEndpoint> endpoints = FilterAspNetEndpoints();

        IList<RouteDescriptor> descriptors = endpoints.Select(RouteDescriptor.FromAspNetEndpoint).ToList();
        RouteMappingsResponse response = RouteMappingsResponse.FromRouteDescriptors(descriptors);

        return Task.FromResult(response);
    }

    private IEnumerable<AspNetEndpoint> FilterAspNetEndpoints()
    {
        IList<AspNetEndpoint> endpoints = _aspNetEndpointProvider.GetEndpoints();
        RouteMappingsEndpointOptions options = _endpointOptionsMonitor.CurrentValue;

        if (options.IncludeActuators)
        {
            return endpoints;
        }

        List<AspNetEndpoint> nonActuatorEndpoints = [];
        PathString[] actuatorEndpointPaths = GetActuatorEndpointPaths().ToArray();

        foreach (AspNetEndpoint endpoint in endpoints)
        {
            if (endpoint.RoutePattern == null || !endpoint.RoutePattern.StartsWith('/') ||
                !Array.Exists(actuatorEndpointPaths, path => path.StartsWithSegments(endpoint.RoutePattern)))
            {
                nonActuatorEndpoints.Add(endpoint);
            }
        }

        return nonActuatorEndpoints;
    }

    private IEnumerable<PathString> GetActuatorEndpointPaths()
    {
        ManagementOptions managementOptions = _managementOptionsMonitor.CurrentValue;

        foreach (EndpointOptions endpointOptions in _endpointOptionsMonitorProviders.Select(provider => provider.Get()))
        {
            string endpointPath = endpointOptions.GetPathMatchPattern(managementOptions.Path);
            yield return endpointPath;

            if (Platform.IsCloudFoundry)
            {
                string cloudFoundryPath = endpointOptions.GetPathMatchPattern(ConfigureManagementOptions.DefaultCloudFoundryPath);
                yield return cloudFoundryPath;
            }
        }
    }
}
