// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Actuators.RouteMappings.ResponseTypes;

public sealed class RouteMappingsContainer
{
    [JsonPropertyName("dispatcherServlets")]
    public RouteDispatcherServlets DispatcherServlets { get; }

    public RouteMappingsContainer(RouteDispatcherServlets dispatcherServlets)
    {
        ArgumentNullException.ThrowIfNull(dispatcherServlets);

        DispatcherServlets = dispatcherServlets;
    }
}
