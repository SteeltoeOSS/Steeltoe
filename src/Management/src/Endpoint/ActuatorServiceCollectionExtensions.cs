// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Configuration;
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
using Steeltoe.Management.Endpoint.Configuration;
using Steeltoe.Management.Endpoint.ManagementPort;

namespace Steeltoe.Management.Endpoint;

public static class ActuatorServiceCollectionExtensions
{
    public static IServiceCollection AddCommonActuatorServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddRouting();
        services.TryAddScoped<ActuatorEndpointMapper>();

        services.ConfigureOptionsWithChangeTokenSource<ManagementOptions, ConfigureManagementOptions>();

        return services;
    }

    internal static void ConfigureEndpointOptions<TOptions, TConfigureOptions>(this IServiceCollection services)
        where TOptions : EndpointOptions
        where TConfigureOptions : class, IConfigureOptionsWithKey<TOptions>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.ConfigureOptionsWithChangeTokenSource<TOptions, TConfigureOptions>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEndpointOptionsMonitorProvider, EndpointOptionsMonitorProvider<TOptions>>());
    }

    internal static void ConfigureOptionsWithChangeTokenSource<TOptions, TConfigureOptions>(this IServiceCollection services)
        where TOptions : class
        where TConfigureOptions : class, IConfigureOptionsWithKey<TOptions>
    {
        services.AddOptions();
        services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<TOptions>, TConfigureOptions>());

        services.TryAddSingleton<IOptionsChangeTokenSource<TOptions>, ConfigurationChangeTokenSource<TOptions>>();
    }

    public static IServiceCollection AddAllActuators(this IServiceCollection services, Action<CorsPolicyBuilder>? buildCorsPolicy)
    {
        return AddAllActuators(services, MediaTypeVersion.V2, buildCorsPolicy);
    }

    public static IServiceCollection AddAllActuators(this IServiceCollection services)
    {
        return AddAllActuators(services, MediaTypeVersion.V2);
    }

    public static IServiceCollection AddAllActuators(this IServiceCollection services, MediaTypeVersion version)
    {
        return AddAllActuators(services, version, null);
    }

    public static IServiceCollection AddAllActuators(this IServiceCollection services, MediaTypeVersion version, Action<CorsPolicyBuilder>? buildCorsPolicy)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSteeltoeCors(buildCorsPolicy);

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

    private static void AddSteeltoeCors(this IServiceCollection services, Action<CorsPolicyBuilder>? buildCorsPolicy = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddCors(setup =>
        {
            setup.AddPolicy("SteeltoeManagement", policy =>
            {
                policy.WithMethods("GET", "POST");

                if (Platform.IsCloudFoundry)
                {
                    policy.WithHeaders("Authorization", "X-Cf-App-Instance", "Content-Type", "Content-Disposition");
                }

                if (buildCorsPolicy != null)
                {
                    buildCorsPolicy(policy);
                }
                else
                {
                    policy.AllowAnyOrigin();
                }
            });
        });
    }

    /// <summary>
    /// Registers <see cref="IStartupFilter" />s that will map all configured actuators, initialize health, etc.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IEndpointConventionBuilder ActivateActuatorEndpoints(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var actuatorConventionBuilder = new ActuatorConventionBuilder();

        services.TryAddEnumerable(
            ServiceDescriptor.Transient<IStartupFilter, AllActuatorsStartupFilter>(_ => new AllActuatorsStartupFilter(actuatorConventionBuilder)));

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter, ManagementPortStartupFilter>());

        return actuatorConventionBuilder;
    }
}
