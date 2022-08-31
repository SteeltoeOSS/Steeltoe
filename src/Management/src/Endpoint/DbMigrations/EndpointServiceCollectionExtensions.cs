// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Hypermedia;

namespace Steeltoe.Management.Endpoint.DbMigrations;

public static class EndpointServiceCollectionExtensions
{
    /// <summary>
    /// Adds components of the Entity Framework actuator to the D/I container.
    /// </summary>
    /// <param name="services">
    /// Service collection to add actuator to.
    /// </param>
    /// <param name="configuration">
    /// Application configuration. Retrieved from the <see cref="IServiceCollection" /> if not provided. (this actuator looks for settings starting with
    /// management:endpoints:dbmigrations).
    /// </param>
    public static void AddDbMigrationsActuator(this IServiceCollection services, IConfiguration configuration = null)
    {
        ArgumentGuard.NotNull(services);

        configuration ??= services.BuildServiceProvider().GetRequiredService<IConfiguration>();

        services.AddActuatorManagementOptions(configuration);
        services.AddDbMigrationsActuatorServices(configuration);
        services.AddActuatorEndpointMapping<DbMigrationsEndpoint>();
    }
}
