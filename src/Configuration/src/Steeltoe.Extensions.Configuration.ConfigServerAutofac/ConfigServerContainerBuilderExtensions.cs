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
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Options.Autofac;
using System;
using System.Linq;

namespace Steeltoe.Extensions.Configuration.ConfigServer
{
    /// <summary>
    /// Extension methods for adding services related to Spring Cloud Config Server.
    /// </summary>
    public static class ConfigServerContainerBuilderExtensions
    {
        public static void RegisterConfigServerClientOptions(this ContainerBuilder container, IConfiguration config)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var section = config.GetSection(ConfigServerClientSettingsOptions.CONFIGURATION_PREFIX);
            container.RegisterOption<ConfigServerClientSettingsOptions>(section);
        }

        /// <summary>
        /// Add the ConfigServerHealthContributor as a IHealthContributor to the container.
        /// Note: You also need to add the applications IConfiguration to the container as well.
        /// </summary>
        /// <param name="container">the autofac container builder</param>
        public static void RegisterConfigServerHealthContributor(this ContainerBuilder container)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            container.RegisterType<ConfigServerHealthContributor>().As<IHealthContributor>().SingleInstance();
        }
    }
}
