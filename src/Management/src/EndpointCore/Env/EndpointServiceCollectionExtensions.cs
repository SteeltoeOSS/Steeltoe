// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Steeltoe.Management.Endpoint.Hypermedia;
using System;

namespace Steeltoe.Management.Endpoint.Env
{
    public static class EndpointServiceCollectionExtensions
    {
        /// <summary>
        /// Adds components of the Env actuator to Microsoft-DI
        /// </summary>
        /// <param name="services">Service collection to add actuator to</param>
        /// <param name="config">Application configuration (this actuator looks for settings starting with management:endpoints:dump)</param>
        public static void AddEnvActuator(this IServiceCollection services, IConfiguration config)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

#if NETCOREAPP3_1
            services.TryAddSingleton<IHostEnvironment>((provider) =>
            {
                var service = provider.GetRequiredService<IHostEnvironment>();
#else
            services.TryAddSingleton<IHostingEnvironment>((provider) =>
            {
                var service = provider.GetRequiredService<IHostingEnvironment>();
#endif
                return new GenericHostingEnvironment()
                {
                    EnvironmentName = service.EnvironmentName,
                    ApplicationName = service.ApplicationName,
                    ContentRootFileProvider = service.ContentRootFileProvider,
                    ContentRootPath = service.ContentRootPath
                };
            });

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IManagementOptions>(new ActuatorManagementOptions(config)));
            var options = new EnvEndpointOptions(config);
            services.TryAddSingleton<IEnvOptions>(options);
            services.RegisterEndpointOptions(options);
            services.TryAddSingleton<EnvEndpoint>();
        }
    }
}
