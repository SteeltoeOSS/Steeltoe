// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
