// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

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
                JsonSerializerOptions serializerOptions = managementOptions.SerializerOptions ?? new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                if (!serializerOptions.Converters.Any(c => c is ThreadDumpV2Converter))
                {
                    serializerOptions.Converters.Add(new ThreadDumpV2Converter());
                }
            });
        }

        services.TryAddSingleton<EventPipeThreadDumper>();

        return services;
    }
}
