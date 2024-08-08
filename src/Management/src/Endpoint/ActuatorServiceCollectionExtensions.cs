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
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.DbMigrations;
using Steeltoe.Management.Endpoint.Environment;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.ManagementPort;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.Refresh;
using Steeltoe.Management.Endpoint.RouteMappings;
using Steeltoe.Management.Endpoint.Services;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Trace;
using Steeltoe.Management.Endpoint.Web.Hypermedia;

namespace Steeltoe.Management.Endpoint;

public static class ActuatorServiceCollectionExtensions
{
    public static void AddCommonActuatorServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<ActuatorEndpointMapper>();

        services.ConfigureOptionsWithChangeTokenSource<ManagementOptions, ConfigureManagementOptions>();
    }

    internal static void ConfigureEndpointOptions<TOptions, TConfigureOptions>(this IServiceCollection services)
        where TOptions : EndpointOptions
        where TConfigureOptions : class, IConfigureOptionsWithKey<TOptions>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.ConfigureOptionsWithChangeTokenSource<TOptions, TConfigureOptions>();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<EndpointOptions, TOptions>(provider => provider.GetRequiredService<IOptionsMonitor<TOptions>>().CurrentValue));
    }

    internal static void ConfigureOptionsWithChangeTokenSource<TOptions, TConfigureOptions>(this IServiceCollection services)
        where TOptions : class
        where TConfigureOptions : class, IConfigureOptionsWithKey<TOptions>
    {
        services.AddOptions();
        services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<TOptions>, TConfigureOptions>());

        services.TryAddSingleton<IOptionsChangeTokenSource<TOptions>, ConfigurationChangeTokenSource<TOptions>>();
    }

    public static void AddAllActuators(this IServiceCollection services, Action<CorsPolicyBuilder>? buildCorsPolicy)
    {
        services.AddAllActuators(MediaTypeVersion.V2, buildCorsPolicy);
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
        services.AddTraceActuator(version);
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
