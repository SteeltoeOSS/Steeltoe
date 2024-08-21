// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management.Endpoint.Configuration;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Actuators.ThreadDump;

/// <summary>
/// Add services used by the ThreadDump actuator.
/// </summary>
internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the services used by the Thread Dump actuator.
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
    public static IServiceCollection AddThreadDumpActuatorServices(this IServiceCollection services, MediaTypeVersion version)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (version == MediaTypeVersion.V1)
        {
            services.ConfigureEndpointOptions<ThreadDumpEndpointOptions, ConfigureThreadDumpEndpointOptionsV1>();
        }
        else
        {
            services.ConfigureEndpointOptions<ThreadDumpEndpointOptions, ConfigureThreadDumpEndpointOptions>();
        }

        services.TryAddSingleton<IThreadDumpEndpointHandler, ThreadDumpEndpointHandler>();

        services.AddSingleton<ThreadDumpEndpointMiddleware>();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEndpointMiddleware, ThreadDumpEndpointMiddleware>());

        if (version == MediaTypeVersion.V2)
        {
            services.PostConfigure((ManagementOptions managementOptions) =>
            {
                JsonSerializerOptions serializerOptions = managementOptions.SerializerOptions;

                if (!serializerOptions.Converters.Any(converter => converter is ThreadDumpV2Converter))
                {
                    serializerOptions.Converters.Add(new ThreadDumpV2Converter());
                }
            });
        }

        services.TryAddSingleton<EventPipeThreadDumper>();

        return services;
    }
}
