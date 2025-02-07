// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Actuators.DbMigrations;
using Steeltoe.Management.Endpoint.Actuators.Environment;
using Steeltoe.Management.Endpoint.Actuators.Health;
using Steeltoe.Management.Endpoint.Actuators.HeapDump;
using Steeltoe.Management.Endpoint.Actuators.HttpExchanges;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Endpoint.Actuators.Info;
using Steeltoe.Management.Endpoint.Actuators.Loggers;
using Steeltoe.Management.Endpoint.Actuators.Refresh;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings;
using Steeltoe.Management.Endpoint.Actuators.Services;
using Steeltoe.Management.Endpoint.Actuators.ThreadDump;

namespace Steeltoe.Management.Endpoint.Actuators.All;

public static class EndpointServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Steeltoe actuators to the service container and configures the ASP.NET Core middleware pipeline.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddAllActuators(this IServiceCollection services)
    {
        return AddAllActuators(services, true);
    }

    /// <summary>
    /// Adds all Steeltoe actuators to the service container.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="configureMiddleware">
    /// When <c>false</c>, skips configuration of the ASP.NET Core middleware pipeline. While this provides full control over the pipeline order, it requires
    /// manual addition of the appropriate middleware for actuators to work correctly.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddAllActuators(this IServiceCollection services, bool configureMiddleware)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (Platform.IsCloudFoundry)
        {
            services.AddCloudFoundryActuator(configureMiddleware);
        }

        services.AddHypermediaActuator(configureMiddleware);
        services.AddThreadDumpActuator(configureMiddleware);
        services.AddHeapDumpActuator(configureMiddleware);
        services.AddDbMigrationsActuator(configureMiddleware);
        services.AddEnvironmentActuator(configureMiddleware);
        services.AddInfoActuator(configureMiddleware);
        services.AddHealthActuator(configureMiddleware);
        services.AddLoggersActuator(configureMiddleware);
        services.AddHttpExchangesActuator(configureMiddleware);
        services.AddRouteMappingsActuator(configureMiddleware);
        services.AddRefreshActuator(configureMiddleware);
        services.AddServicesActuator(configureMiddleware);

        return services;
    }
}
