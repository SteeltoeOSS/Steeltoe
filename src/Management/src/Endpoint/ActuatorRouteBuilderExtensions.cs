// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Management.Endpoint;

public static class ActuatorRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapAllActuators(this IEndpointRouteBuilder builder) =>
        MapAllActuators(builder, null);
    public static IEndpointConventionBuilder MapAllActuators(this IEndpointRouteBuilder endpoints, ActuatorConventionBuilder conventionBuilder)
    {
        IServiceProvider serviceProvider = endpoints.ServiceProvider;

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            var mapper = scope.ServiceProvider.GetService<ActuatorEndpointMapper>();
            mapper.Map(endpoints, ref conventionBuilder);
            return conventionBuilder;
        }
    }
}

public class ActuatorConventionBuilder : IEndpointConventionBuilder
{
    private readonly List<IEndpointConventionBuilder> _builders = new();

    public void Add(Action<EndpointBuilder> convention)
    {
        foreach (IEndpointConventionBuilder builder in _builders)
        {
            builder.Add(convention);
        }
    }

    public void Add(IEndpointConventionBuilder builder)
    {
        _builders.Add(builder);
    }
}
