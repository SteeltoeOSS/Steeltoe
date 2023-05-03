// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register startup/shutdown interactions with Spring Boot Admin server.
    /// </summary>
    /// <param name="services">
    /// Reference to the service collection.
    /// </param>
    /// <returns>
    /// A reference to the service collection.
    /// </returns>
    public static IServiceCollection AddSpringBootAdminClient(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        services.RegisterDefaultApplicationInstanceInfo();
        services.ConfigureOptions<ConfigureManagementEndpointOptions>();
        services.ConfigureOptions<ConfigureHealthEndpointOptions>();
        services.AddSingleton<SpringBootAdminClientOptions>();
        services.AddHostedService<SpringBootAdminClientHostedService>();
        return services;
    }
}
