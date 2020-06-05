// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Mappings;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Trace;
using Steeltoe.Management.Hypermedia;
using System;

namespace Steeltoe.Management.CloudFoundry
{
    public static class CloudFoundryServiceCollectionExtensions
    {
        /// <summary>
        /// Add Actuators to Microsoft DI
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="config">Application Configuration</param>
        /// <param name="buildCorsPolicy">Customize the CORS policy. </param>
        public static void AddCloudFoundryActuators(this IServiceCollection services, IConfiguration config, Action<CorsPolicyBuilder> buildCorsPolicy = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddCloudFoundryActuators(config, MediaTypeVersion.V1, ActuatorContext.CloudFoundry, buildCorsPolicy);
        }

        /// <summary>
        /// Add Actuators to Microsoft DI
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="config">Application Configuration</param>
        /// <param name="version">Set response type version</param>
        /// <param name="context">The context in which to run the actuators</param>
        /// <param name="buildCorsPolicy">Customize the CORS policy. </param>
        public static void AddCloudFoundryActuators(this IServiceCollection services, IConfiguration config, MediaTypeVersion version, ActuatorContext context, Action<CorsPolicyBuilder> buildCorsPolicy = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (context != ActuatorContext.Actuator)
            {
                var managementOptions = new CloudFoundryManagementOptions(config);
                services.TryAddEnumerable(ServiceDescriptor.Singleton<IManagementOptions>(managementOptions));

                services.AddCors(setup =>
                {
                    setup.AddPolicy("SteeltoeManagement", (policy) =>
                        {
                            policy
                                .WithMethods("GET", "POST")
                                .WithHeaders("Authorization", "X-Cf-App-Instance", "Content-Type");

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
                services.AddCloudFoundryActuator(config);
            }

            if (context != ActuatorContext.CloudFoundry)
            {
                services.AddHypermediaActuator(config);
            }

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                services.AddThreadDumpActuator(config, version);
                services.AddHeapDumpActuator(config);
            }

            services.AddInfoActuator(config);
            services.AddHealthActuator(config);
            services.AddLoggersActuator(config);
            services.AddTraceActuator(config, version);
            services.AddMappingsActuator(config);
        }
    }
}
