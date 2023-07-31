// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint;

public static class ActuatorRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapAllActuators(this IEndpointRouteBuilder builder)
    {
        return MapAllActuators(builder, null);
    }

    public static IEndpointConventionBuilder MapAllActuators(this IEndpointRouteBuilder endpoints, ActuatorConventionBuilder conventionBuilder)
    {
        ArgumentGuard.NotNull(endpoints);

        IServiceProvider serviceProvider = endpoints.ServiceProvider;

        using IServiceScope scope = serviceProvider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<ActuatorEndpointMapper>();
        conventionBuilder ??= new ActuatorConventionBuilder();

        mapper.Map(endpoints, conventionBuilder);
        return conventionBuilder;
    }
}
