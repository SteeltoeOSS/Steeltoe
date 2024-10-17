// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.ManagementPort;

namespace Steeltoe.Management.Endpoint;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds a middleware that provides actuator endpoints.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IApplicationBuilder" />.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IApplicationBuilder UseActuators(this IApplicationBuilder builder)
    {
        return UseActuators(builder, null);
    }

    /// <summary>
    /// Adds a middleware that provides actuator endpoints.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IApplicationBuilder" />.
    /// </param>
    /// <param name="configureEndpoints">
    /// Takes an <see cref="IEndpointConventionBuilder" /> to customize the mapped endpoints. Useful for tailoring auth requirements.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IApplicationBuilder UseActuators(this IApplicationBuilder builder, Action<IEndpointConventionBuilder>? configureEndpoints)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var mvcOptions = builder.ApplicationServices.GetService<IOptions<MvcOptions>>();
        bool isEndpointRoutingEnabled = mvcOptions?.Value.EnableEndpointRouting ?? true;

        if (!isEndpointRoutingEnabled && configureEndpoints != null)
        {
            throw new NotSupportedException("Customizing endpoints is only supported when using endpoint routing.");
        }

        if (isEndpointRoutingEnabled)
        {
            builder.UseEndpoints(endpoints =>
            {
                IEndpointConventionBuilder conventionBuilder = endpoints.MapActuators();
                configureEndpoints?.Invoke(conventionBuilder);
            });
        }
        else
        {
            builder.UseMvc(routeBuilder => routeBuilder.MapActuators());
        }

        return builder;
    }

    /// <summary>
    /// Adds a middleware that filters requests based on the configured management port.
    /// </summary>
    /// <remarks>
    /// Because ASP.NET provides no way to vary listen ports per endpoint, the management port is added to the list of listen addresses at startup and
    /// requests are then filtered here by port/path combination.
    /// </remarks>
    /// <param name="builder">
    /// The <see cref="IApplicationBuilder" />.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IApplicationBuilder UseManagementPort(this IApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseMiddleware<ManagementPortMiddleware>();
    }
}
