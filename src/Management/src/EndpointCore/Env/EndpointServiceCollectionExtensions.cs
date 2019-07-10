// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
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

            services.TryAddSingleton<IHostingEnvironment>((provider) =>
            {
                var service = provider.GetRequiredService<Microsoft.AspNetCore.Hosting.IHostingEnvironment>();
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
