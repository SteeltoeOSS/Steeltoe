// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Actuators.RouteMappings.RoutingTypes;

/// <summary>
/// Indicates which ASP.NET subsystem an endpoint originates from.
/// </summary>
internal enum AspNetEndpointSource
{
    /// <summary>
    /// Endpoint originates from API Explorer (Minimal API or API Controller).
    /// </summary>
    ApiExplorer,

    /// <summary>
    /// Endpoint originates from an ASP.NET MVC Controller.
    /// </summary>
    MvcControllerEndpointDataSource,

    /// <summary>
    /// Endpoint originates from ASP.NET Razor Pages.
    /// </summary>
    RazorPagesEndpointDataSource,

    /// <summary>
    /// Endpoint originates from Steeltoe actuator.
    /// </summary>
    ActuatorEndpointDataSource
}
