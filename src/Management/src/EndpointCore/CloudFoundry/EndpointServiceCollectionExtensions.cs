﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Linq;

namespace Steeltoe.Management.Endpoint.CloudFoundry
{
    public static class EndpointServiceCollectionExtensions
    {
        public static void AddCloudFoundryActuator(this IServiceCollection services, IConfiguration config)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IManagementOptions>(new CloudFoundryManagementOptions(config)));

            services.TryAddSingleton<ICloudFoundryOptions>(provider =>
            {
                var mgmtOptions = provider
                    .GetServices<IManagementOptions>().Single(m => m.GetType() == typeof(CloudFoundryManagementOptions));

                var opts = new CloudFoundryEndpointOptions(config);
                mgmtOptions.EndpointOptions.Add(opts);

                return opts;
            });

            services.TryAddSingleton<CloudFoundryEndpoint>();
        }
    }
}
