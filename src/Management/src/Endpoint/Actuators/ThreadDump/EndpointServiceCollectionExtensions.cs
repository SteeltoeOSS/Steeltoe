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
    /// Adds the thread dump actuator to the service container and configures the ASP.NET middleware pipeline.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddThreadDumpActuator(this IServiceCollection services)
    {
        return AddThreadDumpActuator(services, true);
    }

    /// <summary>
    /// Adds the thread dump actuator to the service container.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="configureMiddleware">
    /// When <c>false</c>, skips configuration of the ASP.NET middleware pipeline. While this provides full control over the pipeline order, it requires to
    /// manually add the appropriate middleware for actuators to work correctly.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddThreadDumpActuator(this IServiceCollection services, bool configureMiddleware)
    {
        return AddThreadDumpActuator(services, MediaTypeVersion.V2, configureMiddleware);
    }

    /// <summary>
    /// Adds the thread dump actuator to the service container.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="version">
    /// The media version to use. This also determines where configuration for this actuator is read from.
    /// </param>
    /// <param name="configureMiddleware">
    /// When <c>false</c>, skips configuration of the ASP.NET middleware pipeline. While this provides full control over the pipeline order, it requires to
    /// manually add the appropriate middleware for actuators to work correctly.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddThreadDumpActuator(this IServiceCollection services, MediaTypeVersion version, bool configureMiddleware)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (version == MediaTypeVersion.V1)
        {
            services.AddCoreActuatorServices<ThreadDumpEndpointOptions, ConfigureThreadDumpEndpointOptionsV1, ThreadDumpEndpointMiddleware,
                IThreadDumpEndpointHandler, ThreadDumpEndpointHandler, object?, IList<ThreadInfo>>(configureMiddleware);
        }
        else
        {
            services.AddCoreActuatorServices<ThreadDumpEndpointOptions, ConfigureThreadDumpEndpointOptions, ThreadDumpEndpointMiddleware,
                IThreadDumpEndpointHandler, ThreadDumpEndpointHandler, object?, IList<ThreadInfo>>(configureMiddleware);
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
