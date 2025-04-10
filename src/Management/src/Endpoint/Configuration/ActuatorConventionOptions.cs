// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;

namespace Steeltoe.Management.Endpoint.Configuration;

/// <summary>
/// Stores callbacks, which are applied on the <see cref="IEndpointConventionBuilder" />s returned from mapping actuator endpoints.
/// </summary>
internal sealed class ActuatorConventionOptions
{
    public IList<Action<IEndpointConventionBuilder>> ConfigureActions { get; } = new List<Action<IEndpointConventionBuilder>>();
}
