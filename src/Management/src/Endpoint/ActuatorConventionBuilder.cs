// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;

namespace Steeltoe.Management.Endpoint;

/// <summary>
/// Captures conventions for actuator endpoints and applies them to the builders returned from ASP.NET MapMethods() calls.
/// <para>
/// Used by IApplicationBuilder.UseActuatorEndpoints() and IEndpointRouteBuilder.MapActuators(). <see cref="TrackTarget" /> executes BEFORE any optional
/// <see cref="Add" /> calls.
/// </para>
/// </summary>
internal sealed class ActuatorConventionBuilder : IEndpointConventionBuilder
{
    private readonly List<IEndpointConventionBuilder> _builders = [];

    /// <summary>
    /// Adds a convention to the builder. This interface implementation is called by ASP.NET extension methods, such as
    /// <see cref="AuthorizationEndpointConventionBuilderExtensions.RequireAuthorization{TBuilder}(TBuilder)" />.
    /// </summary>
    /// <param name="convention">
    /// The convention to add.
    /// </param>
    public void Add(Action<EndpointBuilder> convention)
    {
        ArgumentNullException.ThrowIfNull(convention);

        foreach (IEndpointConventionBuilder builder in _builders)
        {
            builder.Add(convention);
        }
    }

    /// <summary>
    /// Captures a convention builder, which is returned from ASP.NET MapMethods() for a single actuator endpoint.
    /// </summary>
    /// <param name="builder">
    /// The builder returned from MapMethods().
    /// </param>
    public void TrackTarget(IEndpointConventionBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        _builders.Add(builder);
    }
}
