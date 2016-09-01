//
// Copyright 2015 the original author or authors.
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
//

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SteelToe.CloudFoundry.Connector.Services;
using System;

namespace SteelToe.CloudFoundry.Connector.OAuth
{
    public static class OAuthServiceCollectionExtensions
    {
        public static IServiceCollection AddOAuthOptions(this IServiceCollection services, IConfiguration config)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            OAuthConnectorOptions redisConfig = new OAuthConnectorOptions(config);
            SsoServiceInfo info = config.GetSingletonServiceInfo<SsoServiceInfo>();
            OAuthConnectorFactory factory = new OAuthConnectorFactory(info, redisConfig); ;
            services.AddSingleton(typeof(IOptions<OAuthServiceOptions>), factory.Create);
            return services;
        }

        public static IServiceCollection AddOAuthOptions(this IServiceCollection services, IConfiguration config, string serviceName)
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

            OAuthConnectorOptions redisConfig = new OAuthConnectorOptions(config);
            SsoServiceInfo info = config.GetRequiredServiceInfo<SsoServiceInfo>(serviceName);
            OAuthConnectorFactory factory = new OAuthConnectorFactory(info, redisConfig);
            services.AddSingleton(typeof(IOptions<OAuthServiceOptions>), factory.Create);
            return services;
        }
    }
}
