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
