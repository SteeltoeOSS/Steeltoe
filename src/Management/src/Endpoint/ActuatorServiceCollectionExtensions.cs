// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.DbMigrations;
using Steeltoe.Management.Endpoint.Env;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Mappings;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Refresh;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Trace;

namespace Steeltoe.Management.Endpoint;

public static class ActuatorServiceCollectionExtensions
{
    public static void AddAllActuators(this IServiceCollection services, IConfiguration configuration, Action<CorsPolicyBuilder> buildCorsPolicy)
    {
        services.AddAllActuators(configuration, MediaTypeVersion.V2, buildCorsPolicy);
    }

    public static IServiceCollection AddAllActuators(this IServiceCollection services, IConfiguration configuration = null,
        MediaTypeVersion version = MediaTypeVersion.V2, Action<CorsPolicyBuilder> buildCorsPolicy = null)
    {
        ArgumentGuard.NotNull(services);

        configuration ??= services.BuildServiceProvider().GetRequiredService<IConfiguration>();

        services.AddSteeltoeCors(buildCorsPolicy);

        if (Platform.IsCloudFoundry)
        {
            services.AddCloudFoundryActuator(configuration);
        }

        services.AddHypermediaActuator(configuration);

        services.AddThreadDumpActuator(configuration, version);

        services.AddHeapDumpActuator(configuration);

        services.AddDbMigrationsActuator(configuration);
        services.AddEnvActuator(configuration);
        services.AddInfoActuator(configuration);
        services.AddHealthActuator(configuration);
        services.AddLoggersActuator(configuration);
        services.AddTraceActuator(configuration, version);
        services.AddMappingsActuator(configuration);
        services.AddMetricsActuator(configuration);
        services.AddPrometheusActuator(configuration);
        services.AddRefreshActuator(configuration);
        return services;
    }

    private static IServiceCollection AddSteeltoeCors(this IServiceCollection services, Action<CorsPolicyBuilder> buildCorsPolicy = null)
    {
        return services.AddCors(setup =>
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
