// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Actuators.RouteMappings.RoutingTypes;

/// <summary>
/// Provides unified information about the origin of a parameter in an ASP.NET endpoint.
/// </summary>
internal enum AspNetEndpointParameterOrigin
{
    // Origin of parameter is unknown.
    Unknown,

    // Parameter originates from route pattern.
    Route,

    // Parameter originates from query string.
    QueryString,

    // Parameter originates from HTTP header.
    Header,

    // Parameter originates from another source, such as the request body or service container.
    Other
}
