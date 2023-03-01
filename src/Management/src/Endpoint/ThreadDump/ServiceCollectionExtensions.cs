// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;

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
    /// <param name="configuration">
    /// Reference to the configuration system.
    /// </param>
    /// <param name="version">
    /// The media version to use.
    /// </param>
    /// <returns>
    /// A reference to the service collection.
    /// </returns>
    public static IServiceCollection AddThreadDumpActuatorServices(this IServiceCollection services, IConfiguration configuration, MediaTypeVersion version)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);

        //var options = new ThreadDumpEndpointOptions(configuration);

        if (version == MediaTypeVersion.V1) // TODO: Fix media type version as part of IOptions
        {

            services.ConfigureOptions<ConfigureThreadDumpEndpointOptionsV1>();
            services.TryAddSingleton<ThreadDumpEndpoint>();
            services.TryAddSingleton<IThreadDumpEndpoint>(provider => provider.GetRequiredService<ThreadDumpEndpoint>());
        }
        else
        {
            services.ConfigureOptions<ConfigureThreadDumpEndpointOptions>();
            services.TryAddSingleton<ThreadDumpEndpointV2>();
            services.TryAddSingleton<IThreadDumpEndpointV2>(provider => provider.GetRequiredService<ThreadDumpEndpointV2>());
        }

        //services.TryAddSingleton<IThreadDumpOptions>(options);
        services.TryAddSingleton<IThreadDumper, ThreadDumperEp>();
       // services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IEndpointOptions), options));

        return services;
    }
}
