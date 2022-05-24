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
using System;
using System.Diagnostics.CodeAnalysis;
using EndpointServiceCollectionExtensions = Steeltoe.Management.Endpoint.HeapDump.EndpointServiceCollectionExtensions;

namespace Steeltoe.Management.Endpoint
{
    public static class ActuatorServiceCollectionExtensions
    {
        [Obsolete("No longer in use. Retained for binary compatibility")]
        [ExcludeFromCodeCoverage]
        public static void RegisterEndpointOptions(this IServiceCollection services, IEndpointOptions options)
        {
            // the code that was running here is now handled in ActuatorRouteBuilderExtensions
        }

        public static void AddAllActuators(this IServiceCollection services, IConfiguration config, Action<CorsPolicyBuilder> buildCorsPolicy)
            => services.AddAllActuators(config, MediaTypeVersion.V2, buildCorsPolicy);

        public static IServiceCollection AddAllActuators(this IServiceCollection services, IConfiguration config = null, MediaTypeVersion version = MediaTypeVersion.V2, Action<CorsPolicyBuilder> buildCorsPolicy = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            config ??= services.BuildServiceProvider().GetService<IConfiguration>();
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            services.AddSteeltoeCors(buildCorsPolicy);
            if (Platform.IsCloudFoundry)
            {
                services.AddCloudFoundryActuator(config);
            }

            services.AddHypermediaActuator(config);
            if (Platform.IsWindows)
            {
                services.AddThreadDumpActuator(config, version);
            }

            services.AddHeapDumpActuator(config);

            services.AddDbMigrationsActuator(config);
            services.AddEnvActuator(config);
            services.AddInfoActuator(config);
            services.AddHealthActuator(config);
            services.AddLoggersActuator(config);
            services.AddTraceActuator(config, version);
            services.AddMappingsActuator(config);
            services.AddMetricsActuator(config);
            services.AddPrometheusActuator(config);
            services.AddRefreshActuator(config);
            return services;
        }

        private static IServiceCollection AddSteeltoeCors(this IServiceCollection services, Action<CorsPolicyBuilder> buildCorsPolicy = null)
            => services.AddCors(setup =>
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
