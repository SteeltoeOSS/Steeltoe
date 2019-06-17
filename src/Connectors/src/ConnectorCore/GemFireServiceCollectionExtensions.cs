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
using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector.Services;
using System;

namespace Steeltoe.CloudFoundry.Connector.GemFire
{
    public static class GemFireServiceCollectionExtensions
    {
        /// <summary>
        /// Add GemFire to a ServiceCollection
        /// </summary>
        /// <param name="services">Service collection to add to</param>
        /// <param name="config">App configuration</param>
        /// <param name="authInitializer">Your class that implements Apache.Geode.Client.IAuthInitializer</param>
        /// <param name="serviceName">Cloud Foundry service binding's name</param>
        /// <param name="contextLifetime">Lifetime of the service to inject</param>
        /// <param name="loggerFactory">logger factory</param>
        /// <returns>IServiceCollection for chaining</returns>
        public static IServiceCollection AddGemFireConnection(this IServiceCollection services, IConfiguration config, Type authInitializer, string serviceName = null, ServiceLifetime contextLifetime = ServiceLifetime.Singleton, ILoggerFactory loggerFactory = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (authInitializer == null)
            {
                throw new ArgumentNullException(nameof(authInitializer), "You must include a type that takes <string>username, <string>password as constructor arguments");
            }

            GemFireServiceInfo info = serviceName == null
                ? config.GetSingletonServiceInfo<GemFireServiceInfo>()
                : config.GetRequiredServiceInfo<GemFireServiceInfo>(serviceName);

            Type cacheFactory = GemFireTypeLocator.CacheFactory;
            Type cache = GemFireTypeLocator.Cache;
            Type poolFactory = GemFireTypeLocator.PoolFactory;

            //// Type regionFactory = GemFireTypeLocator.RegionFactory;

            var gemFireConfig = new GemFireConnectorOptions(config);
            var connectorFactory = new GemFireConnectorFactory(gemFireConfig, info, loggerFactory?.CreateLogger<GemFireConnectorFactory>());

            // TODO: health check
            //// container.RegisterType<RelationalHealthContributor>().As<IHealthContributor>();

            services.Add(new ServiceDescriptor(cacheFactory, c => connectorFactory.CreateCacheFactory(c, authInitializer), contextLifetime));
            services.Add(new ServiceDescriptor(cache, c => connectorFactory.CreateCache(c.GetRequiredService(cacheFactory)), contextLifetime));
            services.Add(new ServiceDescriptor(poolFactory, c => connectorFactory.CreatePoolFactory(c.GetRequiredService(cache)), contextLifetime));

            return services;
        }
    }
}
