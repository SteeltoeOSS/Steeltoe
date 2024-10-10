// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;

namespace Steeltoe.Management.Endpoint;

/// <summary>
/// Used by IEndpointRouteBuilder.MapAllActuators(). <see cref="TrackTarget" /> executes BEFORE any optional <see cref="Add" /> calls.
/// </summary>
internal sealed class ImmediateActuatorConventionBuilder : ActuatorConventionBuilder
{
    private readonly List<IEndpointConventionBuilder> _builders = [];

    /// <inheritdoc />
    public override void Add(Action<EndpointBuilder> convention)
    {
        ArgumentNullException.ThrowIfNull(convention);

        foreach (IEndpointConventionBuilder builder in _builders)
        {
            builder.Add(convention);
        }
    }

    /// <inheritdoc />
    public override void TrackTarget(IEndpointConventionBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        _builders.Add(builder);
    }
}
