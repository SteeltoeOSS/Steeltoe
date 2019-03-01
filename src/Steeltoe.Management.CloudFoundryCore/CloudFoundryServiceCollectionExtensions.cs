// Copyright 2017 the original author or authors.
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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Loggers;
using Steeltoe.Management.Endpoint.Mappings;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Trace;
using Steeltoe.Management.Hypermedia;
using System;

namespace Steeltoe.Management.CloudFoundry
{
    public static class CloudFoundryServiceCollectionExtensions
    {
        /// <summary>
        /// Add Actuators to Microsoft DI
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="config">Application Configuration</param>
        public static void AddCloudFoundryActuators(this IServiceCollection services, IConfiguration config)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddCloudFoundryActuators(config, MediaTypeVersion.V1, ActuatorContext.CloudFoundry);
        }

        public static void AddCloudFoundryActuators(this IServiceCollection services, IConfiguration config, MediaTypeVersion version, ActuatorContext context)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (context != ActuatorContext.Actuator)
            {
                var managmentOptions = new CloudFoundryManagementOptions(config, Platform.IsCloudFoundry);
                services.TryAddEnumerable(ServiceDescriptor.Singleton<IManagementOptions>(managmentOptions));

                services.AddCors();
                services.AddCloudFoundryActuator(config);
            }

            if (context != ActuatorContext.CloudFoundry)
            {
                services.AddHypermediaActuator(config);
            }

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                services.AddThreadDumpActuator(config, version);
                services.AddHeapDumpActuator(config);
            }

            services.AddInfoActuator(config);
            services.AddHealthActuator(config);
            services.AddLoggersActuator(config);
            services.AddTraceActuator(config, version);
            services.AddMappingsActuator(config);
        }
    }
}
