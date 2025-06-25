// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings.ResponseTypes;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings.RoutingTypes;

namespace Steeltoe.Management.Endpoint.Actuators.RouteMappings;

internal sealed class RouteMappingsEndpointHandler : IRouteMappingsEndpointHandler
{
    private readonly IOptionsMonitor<RouteMappingsEndpointOptions> _endpointOptionsMonitor;
    private readonly AspNetEndpointProvider _aspNetEndpointProvider;

    public EndpointOptions Options => _endpointOptionsMonitor.CurrentValue;

    public RouteMappingsEndpointHandler(IOptionsMonitor<RouteMappingsEndpointOptions> endpointOptionsMonitor, AspNetEndpointProvider aspNetEndpointProvider)
    {
        ArgumentNullException.ThrowIfNull(endpointOptionsMonitor);
        ArgumentNullException.ThrowIfNull(aspNetEndpointProvider);

        _endpointOptionsMonitor = endpointOptionsMonitor;
        _aspNetEndpointProvider = aspNetEndpointProvider;
    }

    public Task<RouteMappingsResponse> InvokeAsync(object? argument, CancellationToken cancellationToken)
    {
        IEnumerable<AspNetEndpoint> endpoints = _aspNetEndpointProvider.GetEndpoints(_endpointOptionsMonitor.CurrentValue.IncludeActuators);

        IList<RouteDescriptor> descriptors = endpoints.Select(RouteDescriptor.FromAspNetEndpoint).ToList();
        RouteMappingsResponse response = RouteMappingsResponse.FromRouteDescriptors(descriptors);

        return Task.FromResult(response);
    }
}
