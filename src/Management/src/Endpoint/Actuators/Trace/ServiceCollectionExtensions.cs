// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Actuators.Trace;

/// <summary>
/// Add services used by the Trace actuator.
/// </summary>
internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the services used by the Trace actuator.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="version">
    /// The media version to use.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddTraceActuatorServices(this IServiceCollection services, MediaTypeVersion version)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.ConfigureEndpointOptions<TraceEndpointOptions, ConfigureTraceEndpointOptions>();
        services.AddSingleton<HttpTraceEndpointHandler>();

        services.TryAddSingleton<IHttpTraceEndpointHandler>(provider =>
        {
            var handler = provider.GetRequiredService<HttpTraceEndpointHandler>();
            handler.Version = version;
            return handler;
        });

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEndpointMiddleware, HttpTraceEndpointMiddleware>());
        services.AddSingleton<HttpTraceEndpointMiddleware>();

        return services;
    }
}
