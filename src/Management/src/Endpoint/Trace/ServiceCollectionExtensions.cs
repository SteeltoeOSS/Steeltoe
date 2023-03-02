// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.ThreadDump;

namespace Steeltoe.Management.Endpoint.Trace;

/// <summary>
/// Add services used by the Trace actuator.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the services used by the Trace actuator.
    /// </summary>
    /// <param name="services">
    /// Reference to the service collection.
    /// </param>
    /// <param name="configuration">
    /// Reference to the configuration system.
    /// </param>
    /// <param name="version">
    /// The media version to use.
    /// </param>
    /// <returns>
    /// A reference to the service collection.
    /// </returns>
    public static IServiceCollection AddTraceActuatorServices(this IServiceCollection services, IConfiguration configuration, MediaTypeVersion version)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);
        services.ConfigureOptions<ConfigureTraceEndpointOptions>();
        switch (version)
        {
            case MediaTypeVersion.V1:
                //var options = new TraceEndpointOptions(configuration);
                //services.TryAddSingleton<ITraceOptions>(options);
                services.TryAddSingleton<TraceEndpoint>();
                //  services.TryAddSingleton<ITraceEndpoint>(provider => provider.GetRequiredService<TraceEndpoint>());
                //  services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IEndpointOptions), options));

                services.TryAddEnumerable(ServiceDescriptor.Singleton<IEndpointMiddleware, TraceEndpointMiddleware>());
                services.AddSingleton<TraceEndpointMiddleware>();
                break;
            default:

                services.TryAddSingleton<HttpTraceEndpoint>();
                //var options2 = new HttpTraceEndpointOptions(configuration);
                //services.TryAddSingleton<ITraceOptions>(options2);
                //services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IEndpointOptions), options2));

                services.TryAddEnumerable(ServiceDescriptor.Singleton<IEndpointMiddleware, HttpTraceEndpointMiddleware>());
                services.AddSingleton<HttpTraceEndpointMiddleware>();
                break;
        }


        return services;
    }
}
