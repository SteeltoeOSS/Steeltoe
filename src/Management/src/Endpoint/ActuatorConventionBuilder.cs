// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;

namespace Steeltoe.Management.Endpoint;

/// <summary>
/// Captures conventions for actuator endpoints and applies them to the builders returned from ASP.NET MapMethods() calls.
/// </summary>
internal abstract class ActuatorConventionBuilder : IEndpointConventionBuilder
{
    /// <summary>
    /// Adds a convention to the builder. This interface implementation is called by ASP.NET extension methods, such as
    /// <see cref="AuthorizationEndpointConventionBuilderExtensions.RequireAuthorization{TBuilder}(TBuilder)" />.
    /// </summary>
    /// <param name="convention">
    /// The convention to add.
    /// </param>
    public abstract void Add(Action<EndpointBuilder> convention);

    /// <summary>
    /// Captures a convention builder, which is returned from ASP.NET MapMethods() for a single actuator endpoint.
    /// </summary>
    /// <param name="builder">
    /// The builder returned from MapMethods().
    /// </param>
    public abstract void TrackTarget(IEndpointConventionBuilder builder);
}
