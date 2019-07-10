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
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.EndpointBase.DbMigrations;
using System;

namespace Steeltoe.Management.Endpoint.EntityFramework
{
    public static class EndpointServiceCollectionExtensions
    {
        /// <summary>
        /// Adds components of the Entity Framework actuator to Microsoft-DI
        /// </summary>
        /// <param name="services">Service collection to add actuator to</param>
        /// <param name="config">Application configuration (this actuator looks for settings starting with management:endpoints:entityframework)</param>
        /// <param name="configAction">Configuration action to register DBContexts being exposed</param>
        public static void AddEntityFrameworkActuator(this IServiceCollection services, IConfiguration config, Action<EntityFrameworkActuatorOptionsBuilder> configAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (configAction == null)
            {
                throw new ArgumentNullException(nameof(configAction));
            }

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IManagementOptions>(new ActuatorManagementOptions(config)));
            var optionsBuilder = new EntityFrameworkActuatorOptionsBuilder(config);
            configAction(optionsBuilder);
            var options = optionsBuilder.Build();
            services.TryAddSingleton<IEntityFrameworkOptions>(options);
            services.RegisterEndpointOptions(options);
            services.TryAddSingleton<EntityFrameworkEndpoint>();
        }
    }
}
