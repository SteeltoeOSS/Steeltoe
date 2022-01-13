// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
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
        /// <param name="config">Application configuration. Retrieved from the <see cref="IServiceCollection"/> if not provided (this actuator looks for settings starting with management:endpoints:env)</param>
        public static void AddEnvActuator(this IServiceCollection services, IConfiguration config = null)
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

            services.TryAddSingleton<IHostEnvironment>((provider) =>
            {
                var service = provider.GetRequiredService<IWebHostEnvironment>();
                return new GenericHostingEnvironment()
                {
                    EnvironmentName = service.EnvironmentName,
                    ApplicationName = service.ApplicationName,
                    ContentRootFileProvider = service.ContentRootFileProvider,
                    ContentRootPath = service.ContentRootPath
                };
            });

            services.AddActuatorManagementOptions(config);
            services.AddEnvActuatorServices(config);
            services.AddActuatorEndpointMapping<EnvEndpoint>();
        }
    }
}
