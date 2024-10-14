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
using Steeltoe.Management.Endpoint.Actuators.Metrics;
using Steeltoe.Management.Endpoint.Actuators.Refresh;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings;
using Steeltoe.Management.Endpoint.Actuators.Services;
using Steeltoe.Management.Endpoint.Actuators.ThreadDump;

namespace Steeltoe.Management.Endpoint.Actuators.All;

public static class EndpointServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Steeltoe actuators to the service container.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddAllActuators(this IServiceCollection services)
    {
        return AddAllActuators(services, MediaTypeVersion.V2);
    }

    /// <summary>
    /// Adds all Steeltoe actuators to the service container.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="version">
    /// The default media version that is used by actuators that support multiple versions.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddAllActuators(this IServiceCollection services, MediaTypeVersion version)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (Platform.IsCloudFoundry)
        {
            services.AddCloudFoundryActuator();
            services.AddCloudFoundrySecurity();
        }

        services.AddHypermediaActuator();
        services.AddThreadDumpActuator(version);
        services.AddHeapDumpActuator();
        services.AddDbMigrationsActuator();
        services.AddEnvironmentActuator();
        services.AddInfoActuator();
        services.AddHealthActuator();
        services.AddLoggersActuator();
        services.AddHttpExchangesActuator();
        services.AddMappingsActuator();
        services.AddMetricsActuator();
        services.AddRefreshActuator();
        services.AddServicesActuator();

        return services;
    }
}
