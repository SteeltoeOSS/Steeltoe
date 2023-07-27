// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint;

public sealed class ActuatorConventionBuilder : IEndpointConventionBuilder
{
    private readonly List<IEndpointConventionBuilder> _builders = new();

    public void Add(Action<EndpointBuilder> convention)
    {
        ArgumentGuard.NotNull(convention);

        foreach (IEndpointConventionBuilder builder in _builders)
        {
            builder.Add(convention);
        }
    }

    public void Add(IEndpointConventionBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        _builders.Add(builder);
    }
}
