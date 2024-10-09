// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;

namespace Steeltoe.Management.Endpoint;

/// <summary>
/// Used by IServiceCollection.ActivateActuatorEndpoints() and host builder extension methods. <see cref="TrackTarget" /> executes AFTER all
/// <see cref="Add" /> and <see cref="AddConfigure" /> calls.
/// </summary>
internal sealed class DeferredActuatorConventionBuilder : ActuatorConventionBuilder
{
    private readonly List<Action<EndpointBuilder>> _conventions = [];
    private readonly List<Action<IEndpointConventionBuilder>> _configureActions = [];

    /// <inheritdoc />
    public override void Add(Action<EndpointBuilder> convention)
    {
        ArgumentNullException.ThrowIfNull(convention);

        _conventions.Add(convention);
    }

    /// <inheritdoc />
    public override void TrackTarget(IEndpointConventionBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        foreach (Action<IEndpointConventionBuilder> configureAction in _configureActions)
        {
            configureAction(builder);
        }

        foreach (Action<EndpointBuilder> convention in _conventions)
        {
            builder.Add(convention);
        }
    }

    /// <summary>
    /// Captures a callback to configure endpoint conventions.
    /// </summary>
    /// <param name="configureConvention">
    /// A callback to configure endpoint conventions, for example: configureEndpoints => configureEndpoints.RequireAuthorization().
    /// </param>
    public void AddConfigure(Action<IEndpointConventionBuilder> configureConvention)
    {
        ArgumentNullException.ThrowIfNull(configureConvention);

        _configureActions.Add(configureConvention);
    }
}
