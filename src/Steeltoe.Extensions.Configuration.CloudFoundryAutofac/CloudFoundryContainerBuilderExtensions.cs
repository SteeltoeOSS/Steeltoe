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

using Autofac;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Options.Autofac;
using System;

namespace Steeltoe.Extensions.Configuration.CloudFoundry
{
    /// <summary>
    /// Extension methods for adding services related to CloudFoundry
    /// </summary>
    public static class CloudFoundryContainerBuilderExtensions
    {
        public static void RegisterCloudFoundryOptions(this ContainerBuilder container, IConfiguration config)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var appSection = config.GetSection(CloudFoundryApplicationOptions.CONFIGURATION_PREFIX);
            container.RegisterOption<CloudFoundryApplicationOptions>(appSection);

            var serviceSection = config.GetSection(CloudFoundryServicesOptions.CONFIGURATION_PREFIX);
            container.RegisterOption<CloudFoundryServicesOptions>(serviceSection);
        }
    }
}
