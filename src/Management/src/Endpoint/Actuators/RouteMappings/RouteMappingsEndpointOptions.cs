// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.RouteMappings;

public sealed class RouteMappingsEndpointOptions : EndpointOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to include actuator endpoints in the route mappings response. Default value: true.
    /// </summary>
    public bool IncludeActuators { get; set; } = true;
}
