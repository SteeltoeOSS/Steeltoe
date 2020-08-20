// Licensed to the .NET Foundation under one or more agreements.
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
        public static void AddCloudFoundryActuator(this IServiceCollection services, IConfiguration config = null)
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

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IManagementOptions>(new CloudFoundryManagementOptions(config)));
            services.TryAddSingleton(provider => provider.GetServices<IManagementOptions>().OfType<CloudFoundryManagementOptions>().First());

            services.TryAddSingleton<ICloudFoundryOptions>(new CloudFoundryEndpointOptions(config));

            services.TryAddSingleton(provider =>
            {
                var options = provider.GetService<ICloudFoundryOptions>();
                var mgmtOptions = provider.GetServices<IManagementOptions>().OfType<CloudFoundryManagementOptions>().SingleOrDefault();
                mgmtOptions.EndpointOptions.Add(options);

                return new CloudFoundryEndpoint(options, mgmtOptions);
            });
        }
    }
}
