// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
