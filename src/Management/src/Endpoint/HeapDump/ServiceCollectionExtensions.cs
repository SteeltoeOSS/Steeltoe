// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.DbMigrations;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.HeapDump;

/// <summary>
/// Add services used by the HeapDump actuator.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the services used by the Heap Dump actuator.
    /// </summary>
    /// <param name="services">
    /// Reference to the service collection.
    /// </param>
    /// <param name="configuration">
    /// Reference to the configuration system.
    /// </param>
    /// <returns>
    /// A reference to the service collection.
    /// </returns>
    public static IServiceCollection AddHeapDumpActuatorServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);

        //var options = new HeapDumpEndpointOptions(configuration);
        //services.TryAddSingleton<IHeapDumpOptions>(options);
        //services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IEndpointOptions), options));
        services.ConfigureOptions<ConfigureHeapDumpEndpointOptions>();
        services.TryAddSingleton<HeapDumpEndpoint>();
        services.TryAddSingleton<IHeapDumpEndpoint>(provider => provider.GetRequiredService<HeapDumpEndpoint>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEndpointMiddleware, HeapDumpEndpointMiddleware>());
        services.AddSingleton<HeapDumpEndpointMiddleware>();

        return services;
    }
}
