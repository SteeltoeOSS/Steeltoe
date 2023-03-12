// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.ThreadDump;

/// <summary>
/// Add services used by the ThreadDump actuator.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the services used by the Thread Dump actuator.
    /// </summary>
    /// <param name="services">
    /// Reference to the service collection.
    /// </param>
    /// <param name="version">
    /// The media version to use.
    /// </param>
    /// <returns>
    /// A reference to the service collection.
    /// </returns>
    public static IServiceCollection AddThreadDumpActuatorServices(this IServiceCollection services, MediaTypeVersion version)
    {
        ArgumentGuard.NotNull(services);

        if (version == MediaTypeVersion.V1) 
        {
            services.ConfigureEndpointOptions<ThreadDumpEndpointOptions, ConfigureThreadDumpEndpointOptionsV1>();
            services.TryAddSingleton<ThreadDumpEndpoint>();
            services.TryAddSingleton<IThreadDumpEndpoint>(provider => provider.GetRequiredService<ThreadDumpEndpoint>());

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IEndpointMiddleware, ThreadDumpEndpointMiddleware>());
            services.AddSingleton<ThreadDumpEndpointMiddleware>();
        }
        else
        {
            services.ConfigureEndpointOptions<ThreadDumpEndpointOptions, ConfigureThreadDumpEndpointOptions>();

            services.TryAddSingleton<ThreadDumpEndpointV2>();
            services.TryAddSingleton<IThreadDumpEndpointV2>(provider => provider.GetRequiredService<ThreadDumpEndpointV2>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IEndpointMiddleware, ThreadDumpEndpointMiddlewareV2>());
            services.AddSingleton<ThreadDumpEndpointMiddlewareV2>();

        }

        services.TryAddSingleton<IThreadDumper, ThreadDumperEp>();

        return services;
    }
}
