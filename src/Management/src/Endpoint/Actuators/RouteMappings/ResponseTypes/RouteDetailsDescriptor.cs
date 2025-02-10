// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings.RoutingTypes;

namespace Steeltoe.Management.Endpoint.Actuators.RouteMappings.ResponseTypes;

public sealed class RouteDetailsDescriptor
{
    [JsonPropertyName("handlerMethod")]
    public RouteHandlerDescriptor? HandlerMethod { get; }

    [JsonPropertyName("requestMappingConditions")]
    public RouteConditionsDescriptor RequestMappingConditions { get; }

    public RouteDetailsDescriptor(RouteConditionsDescriptor requestMappingConditions, RouteHandlerDescriptor? handlerMethod)
    {
        ArgumentNullException.ThrowIfNull(requestMappingConditions);

        RequestMappingConditions = requestMappingConditions;
        HandlerMethod = handlerMethod;
    }

    internal static RouteDetailsDescriptor FromAspNetEndpoint(AspNetEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        RouteConditionsDescriptor conditions = RouteConditionsDescriptor.FromAspNetEndpoint(endpoint);
        RouteHandlerDescriptor? handler = endpoint.HandlerMethod != null ? new RouteHandlerDescriptor(endpoint.HandlerMethod) : null;

        return new RouteDetailsDescriptor(conditions, handler);
    }
}
