// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.CloudFoundry.Connector.Services;
using System;

namespace Steeltoe.CloudFoundry.Connector.OAuth
{
    public static class OAuthServiceCollectionExtensions
    {
        /// <summary>
        /// Adds OAuthServiceOptions to Service Collection
        /// </summary>
        /// <param name="services">Your Service Collection</param>
        /// <param name="config">Application Configuration</param>
        /// <returns>IServiceCollection for chaining</returns>
        public static IServiceCollection AddOAuthServiceOptions(this IServiceCollection services, IConfiguration config)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            OAuthConnectorOptions oauthConfig = new OAuthConnectorOptions(config);
            SsoServiceInfo info = config.GetSingletonServiceInfo<SsoServiceInfo>();
            OAuthConnectorFactory factory = new OAuthConnectorFactory(info, oauthConfig);
            services.AddSingleton(typeof(IOptions<OAuthServiceOptions>), factory.Create);
            return services;
        }

        /// <summary>
        /// Adds OAuthServiceOptions to Service Collection
        /// </summary>
        /// <param name="services">Your Service Collection</param>
        /// <param name="config">Application Configuration</param>
        /// <param name="serviceName">Cloud Foundry service name binding</param>
        /// <returns>IServiceCollection for chaining</returns>
        public static IServiceCollection AddOAuthServiceOptions(this IServiceCollection services, IConfiguration config, string serviceName)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentNullException(nameof(serviceName));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            OAuthConnectorOptions oauthConfig = new OAuthConnectorOptions(config);
            SsoServiceInfo info = config.GetRequiredServiceInfo<SsoServiceInfo>(serviceName);
            OAuthConnectorFactory factory = new OAuthConnectorFactory(info, oauthConfig);
            services.AddSingleton(typeof(IOptions<OAuthServiceOptions>), factory.Create);
            return services;
        }
    }
}
