// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Configuration;
using System;

namespace Steeltoe.Common.Configuration.Autofac
{
    /// <summary>
    /// Extension methods for registering IConfiguration with the Autofac container
    /// </summary>
    public static class ConfigurationContainerBuilderExtensions
    {
        /// <summary>
        /// Register IConfiguration and IConfgurationRoot with the Autofac container
        /// </summary>
        /// <param name="container">the container builder to register with</param>
        /// <param name="configuration">the configuration instance to add to the container</param>
        public static void RegisterConfiguration(this ContainerBuilder container, IConfiguration configuration)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            container.RegisterInstance(configuration).As<IConfigurationRoot>().SingleInstance();
            container.RegisterInstance(configuration).As<IConfiguration>().SingleInstance();
        }
    }
}
