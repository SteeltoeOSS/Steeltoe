// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;

namespace Steeltoe.Management.Endpoint;

/// <summary>
/// Represents a collection of ConventionBuilders which need the same convention applied to all of them.
/// </summary>
public class EndpointCollectionConventionBuilder : IEndpointConventionBuilder
{
    private readonly List<IEndpointConventionBuilder> _conventionBuilders = new ();

    public void AddConventionBuilder(IEndpointConventionBuilder builder)
    {
        _conventionBuilders.Add(builder);
    }

    public void Add(Action<EndpointBuilder> convention)
    {
        foreach (var conventionBuilder in _conventionBuilders)
        {
            conventionBuilder.Add(convention);
        }
    }
}
