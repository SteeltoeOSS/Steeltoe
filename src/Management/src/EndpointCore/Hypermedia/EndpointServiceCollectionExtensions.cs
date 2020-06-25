// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management.Endpoint.CloudFoundry;
using System;
using System.Linq;

namespace Steeltoe.Management.Endpoint.Hypermedia
{
    public static class EndpointServiceCollectionExtensions
    {
        public static void AddHypermediaActuator(this IServiceCollection services, IConfiguration config)
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
            services.TryAddSingleton<IActuatorHypermediaOptions>(provider =>
                {
                    var mgmtOptions = provider
                        .GetServices<IManagementOptions>().Single(m => m.GetType() == typeof(ActuatorManagementOptions));
                    var opts = new HypermediaEndpointOptions(config);
                    mgmtOptions.EndpointOptions.Add(opts);
                    return opts;
                });

            services.TryAddSingleton<ActuatorEndpoint>();
        }

        public static void AddActuatorManagementOptions(this IServiceCollection services, IConfiguration config)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IManagementOptions>(new ActuatorManagementOptions(config)));
            services.TryAddSingleton(provider => provider.GetServices<IManagementOptions>().OfType<ActuatorManagementOptions>().First());
        }
    }
}
