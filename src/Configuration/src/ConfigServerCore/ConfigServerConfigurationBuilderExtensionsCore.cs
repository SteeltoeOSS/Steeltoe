// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace Steeltoe.Extensions.Configuration.ConfigServer
{
    /// <summary>
    /// Extension methods for adding <see cref="ConfigServerConfigurationProvider"/>.
    /// </summary>
    public static class ConfigServerConfigurationBuilderExtensionsCore
    {
#if NETCOREAPP3_0
        [Obsolete("IHostingEnvironment is obsolete")]
#endif
        public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, IHostingEnvironment environment, ILoggerFactory logFactory = null)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            return DoAddConfigServer(configurationBuilder, environment.ApplicationName, environment.EnvironmentName, logFactory);
        }

#if NETCOREAPP3_0
        [Obsolete("IHostingEnvironment is obsolete")]
#endif
        public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, Microsoft.AspNetCore.Hosting.IHostingEnvironment environment, ILoggerFactory logFactory = null)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            return DoAddConfigServer(configurationBuilder, environment.ApplicationName, environment.EnvironmentName, logFactory);
        }

#if NETCOREAPP3_0
        public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, IHostEnvironment environment, ILoggerFactory logFactory = null)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            return DoAddConfigServer(configurationBuilder, environment.ApplicationName, environment.EnvironmentName, logFactory);
        }
#endif

        private static IConfigurationBuilder DoAddConfigServer(IConfigurationBuilder configurationBuilder, string applicationName, string environmentName, ILoggerFactory logFactory)
        {
            if (configurationBuilder == null)
            {
                throw new ArgumentNullException(nameof(configurationBuilder));
            }

            var settings = new ConfigServerClientSettings()
            {
                Name = applicationName,
                Environment = environmentName
            };

            return configurationBuilder.AddConfigServer(settings, logFactory);
        }
    }
}
