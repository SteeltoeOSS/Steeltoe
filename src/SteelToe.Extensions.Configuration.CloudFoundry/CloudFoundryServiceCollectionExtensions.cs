//
// Copyright 2015 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SteelToe.Extensions.Configuration.CloudFoundry;
using System;


namespace SteelToe.Extensions.Configuration
{
    /// <summary>
    /// Extension methods for adding services related to CloudFoundry
    /// </summary>
    public static class CloudFoundryServiceCollectionExtensions
    {
        /// <summary>
        /// A convenience extension method that can optionally be used to add Spring Cloud Config Server client services to the 
        /// ServiceCollection. It adds the IOptions service to the IServiceCollection and then configures IOption service
        /// with CloudFoundryApplicationOptions and CloudFoundryServicesOptions.  
        /// It also adds the IConfigurationRoot as a service instance to the collection.  
        /// After a call to this method, you will be able to use the DI mechanism to get access to all of these components.
        /// </summary>
        /// <param name="services">the service collection to add the services to (required)</param>
        /// <param name="config">the Iconfiguration root (required)</param>
        /// <returns>update IServiceCollection</returns>
        public static IServiceCollection AddCloudFoundry(this IServiceCollection services, IConfigurationRoot config)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            services.AddOptions();
            services.Configure<CloudFoundryApplicationOptions>(config);
            services.Configure<CloudFoundryServicesOptions>(config);
            services.AddSingleton<IConfigurationRoot>(config);
            return services;
        }
    }
}
