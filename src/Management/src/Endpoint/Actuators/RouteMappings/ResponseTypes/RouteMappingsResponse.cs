// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Actuators.RouteMappings.ResponseTypes;

public sealed class RouteMappingsResponse
{
    // See https://docs.spring.io/spring-boot/api/rest/actuator/mappings.html for the response structure in Spring.

    [JsonPropertyName("contexts")]
    public RouteMappingContexts Contexts { get; }

    public RouteMappingsResponse(RouteMappingContexts contexts)
    {
        ArgumentNullException.ThrowIfNull(contexts);

        Contexts = contexts;
    }

    internal static RouteMappingsResponse FromRouteDescriptors(IList<RouteDescriptor> routeDescriptors)
    {
        ArgumentNullException.ThrowIfNull(routeDescriptors);

        return new RouteMappingsResponse(
            new RouteMappingContexts(new RouteMappingContext(new RouteMappingsContainer(new RouteDispatcherServlets(routeDescriptors)), null)));
    }
}
