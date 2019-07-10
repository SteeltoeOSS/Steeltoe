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
