// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.DbMigrations;

/// <summary>
/// Add services used by the DB Migrations actuator.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the services used by the DB Migrations actuator.
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
    public static IServiceCollection AddDbMigrationsActuatorServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);

        var options = new DbMigrationsEndpointOptions(configuration);
        services.TryAddSingleton<IDbMigrationsOptions>(options);
        services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IEndpointOptions), options));
        services.TryAddSingleton<DbMigrationsEndpoint>();
        services.TryAddSingleton<IDbMigrationsEndpoint>(provider => provider.GetRequiredService<DbMigrationsEndpoint>());

        return services;
    }
}
