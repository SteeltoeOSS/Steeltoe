﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management;
using Steeltoe.Management.Endpoint.Mappings;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Add services used by the Mappings actuator
    /// </summary>
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the services used by the Mappings actuator
        /// </summary>
        /// <param name="services">Reference to the service collection</param>
        /// <param name="configuration">Reference to the configuration system</param>
        /// <returns>A reference to the service collection</returns>
        public static IServiceCollection AddMappingsActuatorServices(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var options = new MappingsEndpointOptions(configuration);
            services.TryAddSingleton<IMappingsOptions>(options);
            services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IEndpointOptions), options));
            services.TryAddSingleton<MappingsEndpoint>();

            return services;
        }
    }
}
