// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Cors.Infrastructure;
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
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.Refresh;
using Steeltoe.Management.Endpoint.RouteMappings;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Trace;
using Steeltoe.Management.Endpoint.Web.Hypermedia;

namespace Steeltoe.Management.Endpoint;

public static class ActuatorServiceCollectionExtensions
{
    public static void AddCommonActuatorServices(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        services.TryAddScoped<ActuatorEndpointMapper>();
        services.ConfigureOptions<ConfigureManagementOptions>();
    }

    public static void ConfigureEndpointOptions<TOptions, TConfigureOptions>(this IServiceCollection services)
        where TOptions : EndpointOptions
        where TConfigureOptions : class
    {
        ArgumentGuard.NotNull(services);

        services.ConfigureOptions<TConfigureOptions>();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<EndpointOptions, TOptions>(provider => provider.GetRequiredService<IOptionsMonitor<TOptions>>().CurrentValue));
    }

    public static void AddAllActuators(this IServiceCollection services, Action<CorsPolicyBuilder> buildCorsPolicy)
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

    public static IServiceCollection AddAllActuators(this IServiceCollection services, MediaTypeVersion version, Action<CorsPolicyBuilder> buildCorsPolicy)
    {
        ArgumentGuard.NotNull(services);

        services.AddSteeltoeCors(buildCorsPolicy);

        if (Platform.IsCloudFoundry)
        {
            services.AddCloudFoundryActuator();
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
        return services;
    }

    private static void AddSteeltoeCors(this IServiceCollection services, Action<CorsPolicyBuilder> buildCorsPolicy = null)
    {
        ArgumentGuard.NotNull(services);

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
}
