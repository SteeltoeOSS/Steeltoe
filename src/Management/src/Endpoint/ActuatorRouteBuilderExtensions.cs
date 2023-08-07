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
    /// <summary>
    /// Maps all actuators, when using ASP.NET attribute-based endpoint routing.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IEndpointRouteBuilder" /> to add routes to.
    /// </param>
    /// <returns>
    /// An <see cref="IEndpointConventionBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static IEndpointConventionBuilder MapAllActuators(this IEndpointRouteBuilder builder)
    {
        return MapAllActuators(builder, null);
    }

    /// <summary>
    /// Maps all actuators, when using ASP.NET attribute-based endpoint routing.
    /// </summary>
    /// <param name="routeBuilder">
    /// The <see cref="IEndpointRouteBuilder" /> to add routes to.
    /// </param>
    /// <param name="conventionBuilder">
    /// An optional builder to customize endpoints.
    /// </param>
    /// <returns>
    /// The <see cref="IEndpointConventionBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static IEndpointConventionBuilder MapAllActuators(this IEndpointRouteBuilder routeBuilder, ActuatorConventionBuilder? conventionBuilder)
    {
        ArgumentGuard.NotNull(routeBuilder);

        IServiceProvider serviceProvider = routeBuilder.ServiceProvider;

        using IServiceScope scope = serviceProvider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<ActuatorEndpointMapper>();
        conventionBuilder ??= new ActuatorConventionBuilder();

        mapper.Map(routeBuilder, conventionBuilder);
        return conventionBuilder;
    }

    /// <summary>
    /// Maps all actuators, when using ASP.NET conventional routing.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRouteBuilder" /> to add routes to.
    /// </param>
    /// <returns>
    /// The <see cref="IRouteBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static IRouteBuilder MapAllActuators(this IRouteBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        IServiceProvider serviceProvider = builder.ServiceProvider;

        using IServiceScope scope = serviceProvider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<ActuatorEndpointMapper>();

        mapper.Map(builder);
        return builder;
    }
}
