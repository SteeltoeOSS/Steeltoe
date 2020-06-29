﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management.Endpoint.Hypermedia;
using System;

namespace Steeltoe.Management.Endpoint.Mappings
{
    public static class EndpointServiceCollectionExtensions
    {
        /// <summary>
        /// Adds components of the Mappings actuator to Microsoft-DI
        /// </summary>
        /// <param name="services">Service collection to add actuator to</param>
        /// <param name="config">Application configuration (this actuator looks for settings starting with management:endpoints:dump)</param>
        public static void AddMappingsActuator(this IServiceCollection services, IConfiguration config)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            services.AddActuatorManagementOptions(config);
            var options = new MappingsEndpointOptions(config);
            services.TryAddSingleton<IMappingsOptions>(options);
            services.RegisterEndpointOptions(options);
            services.TryAddSingleton<IRouteMappings, RouteMappings>();
            services.TryAddSingleton<MappingsEndpoint>();
        }
    }
}
