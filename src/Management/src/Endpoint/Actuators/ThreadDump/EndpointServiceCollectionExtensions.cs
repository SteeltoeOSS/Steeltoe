// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.ThreadDump;

public static class EndpointServiceCollectionExtensions
{
    /// <summary>
    /// Adds the thread dump actuator to the service container.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddThreadDumpActuator(this IServiceCollection services)
    {
        return AddThreadDumpActuator(services, MediaTypeVersion.V2);
    }

    /// <summary>
    /// Adds the thread dump actuator to the service container.
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
    public static IServiceCollection AddThreadDumpActuator(this IServiceCollection services, MediaTypeVersion version)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (version == MediaTypeVersion.V1)
        {
            services
                .AddCoreActuatorServicesAsSingleton<ThreadDumpEndpointOptions, ConfigureThreadDumpEndpointOptionsV1, ThreadDumpEndpointMiddleware,
                    IThreadDumpEndpointHandler, ThreadDumpEndpointHandler, object?, IList<ThreadInfo>>();
        }
        else
        {
            services
                .AddCoreActuatorServicesAsSingleton<ThreadDumpEndpointOptions, ConfigureThreadDumpEndpointOptions, ThreadDumpEndpointMiddleware,
                    IThreadDumpEndpointHandler, ThreadDumpEndpointHandler, object?, IList<ThreadInfo>>();
        }

        RegisterJsonConverter(services, version);
        services.TryAddSingleton<EventPipeThreadDumper>();

        return services;
    }

    private static void RegisterJsonConverter(IServiceCollection services, MediaTypeVersion version)
    {
        if (version == MediaTypeVersion.V2)
        {
            services.PostConfigure<ManagementOptions>(managementOptions =>
            {
                if (!managementOptions.SerializerOptions.Converters.Any(converter => converter is ThreadDumpV2Converter))
                {
                    managementOptions.SerializerOptions.Converters.Add(new ThreadDumpV2Converter());
                }
            });
        }
    }
}
