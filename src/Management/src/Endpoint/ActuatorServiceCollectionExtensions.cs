// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
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
using Steeltoe.Management.Endpoint.Services;
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

        services.ConfigureOptionsWithChangeTokenSource<ManagementOptions, ConfigureManagementOptions>();
    }

    internal static void ConfigureEndpointOptions<TOptions, TConfigureOptions>(this IServiceCollection services)
        where TOptions : EndpointOptions
        where TConfigureOptions : class, IConfigureOptionsWithKey<TOptions>
    {
        ArgumentGuard.NotNull(services);

        services.ConfigureOptionsWithChangeTokenSource<TOptions, TConfigureOptions>();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<EndpointOptions, TOptions>(provider => provider.GetRequiredService<IOptionsMonitor<TOptions>>().CurrentValue));
    }

    internal static void ConfigureOptionsWithChangeTokenSource<TOptions, TConfigureOptions>(this IServiceCollection services)
        where TOptions : class
        where TConfigureOptions : class, IConfigureOptionsWithKey<TOptions>
    {
        // Workaround for services.ConfigureOptions<TConfigureOptions>() registering multiple times,
        // see https://github.com/dotnet/runtime/issues/42358.

        services.AddOptions();
        services.TryAddTransient<IConfigureOptions<TOptions>, TConfigureOptions>();

        services.AddSingleton<IOptionsChangeTokenSource<TOptions>>(provider =>
        {
            var configurer = (TConfigureOptions)provider.GetRequiredService<IConfigureOptions<TOptions>>();
            var configuration = provider.GetRequiredService<IConfiguration>();
            return new ConfigurationChangeTokenSource<TOptions>(configuration.GetSection(configurer.ConfigurationKey));
        });
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
        services.AddServicesActuator();
        return services;
    }

    private static void AddSteeltoeCors(this IServiceCollection services, Action<CorsPolicyBuilder>? buildCorsPolicy = null)
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
