// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.CloudFoundry;
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

            services.AddCloudFoundryActuators(config, MediaTypeVersion.V2, buildCorsPolicy);
        }

        /// <summary>
        /// Add Actuators to Microsoft DI
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="config">Application Configuration</param>
        /// <param name="version">Set response type version</param>
        /// <param name="buildCorsPolicy">Customize the CORS policy. </param>
        public static void AddCloudFoundryActuators(this IServiceCollection services, IConfiguration config, MediaTypeVersion version, Action<CorsPolicyBuilder> buildCorsPolicy = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

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
            services.AddAllActuators(config, version);
        }
    }
}
