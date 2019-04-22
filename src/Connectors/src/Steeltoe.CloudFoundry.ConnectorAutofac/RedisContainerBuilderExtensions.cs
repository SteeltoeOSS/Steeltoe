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
using Autofac.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector;
using Steeltoe.CloudFoundry.Connector.Redis;
using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.Common.HealthChecks;
using System;
using System.Reflection;

namespace Steeltoe.CloudFoundry.ConnectorAutofac
{
    public static class RedisContainerBuilderExtensions
    {
        /// <summary>
        /// Adds RedisCache (as IDistributedCache and RedisCache) to your Autofac Container
        /// </summary>
        /// <param name="container">Your Autofac Container Builder</param>
        /// <param name="config">Application configuration</param>
        /// <param name="serviceName">Cloud Foundry service name binding</param>
        /// <returns>the RegistrationBuilder for (optional) additional configuration</returns>
        public static IRegistrationBuilder<object, SimpleActivatorData, SingleRegistrationStyle> RegisterDistributedRedisCache(
            this ContainerBuilder container,
            IConfiguration config,
            string serviceName = null)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            Type redisInterface = RedisTypeLocator.MicrosoftInterface;
            Type redisImplementation = RedisTypeLocator.MicrosoftImplementation;
            Type redisOptions = RedisTypeLocator.MicrosoftOptions;

            RedisServiceInfo info = serviceName != null
                ? config.GetRequiredServiceInfo<RedisServiceInfo>(serviceName)
                : config.GetSingletonServiceInfo<RedisServiceInfo>();

            RedisCacheConnectorOptions redisConfig = new RedisCacheConnectorOptions(config);
            RedisServiceConnectorFactory factory = new RedisServiceConnectorFactory(info, redisConfig, redisImplementation, redisOptions, null);
            container.Register(c => new RedisHealthContributor(factory, redisImplementation, c.ResolveOptional<ILogger<RedisHealthContributor>>())).As<IHealthContributor>();
            return container.Register(c => factory.Create(null)).As(redisInterface, redisImplementation);
        }

        /// <summary>
        /// Adds ConnectionMultiplexer (as ConnectionMultiplexer and IConnectionMultiplexer) to your Autofac Container
        /// </summary>
        /// <param name="container">Your Autofac Container Builder</param>
        /// <param name="config">Application configuration</param>
        /// <param name="serviceName">Cloud Foundry service name binding</param>
        /// <returns>the RegistrationBuilder for (optional) additional configuration</returns>
        public static IRegistrationBuilder<object, SimpleActivatorData, SingleRegistrationStyle> RegisterRedisConnectionMultiplexer(
            this ContainerBuilder container,
            IConfiguration config,
            string serviceName = null)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            Type redisInterface = RedisTypeLocator.StackExchangeInterface;
            Type redisImplementation = RedisTypeLocator.StackExchangeImplementation;
            Type redisOptions = RedisTypeLocator.StackExchangeOptions;
            MethodInfo initializer = RedisTypeLocator.StackExchangeInitializer;

            RedisServiceInfo info = serviceName != null
                ? config.GetRequiredServiceInfo<RedisServiceInfo>(serviceName)
                : config.GetSingletonServiceInfo<RedisServiceInfo>();

            RedisCacheConnectorOptions redisConfig = new RedisCacheConnectorOptions(config);
            RedisServiceConnectorFactory factory = new RedisServiceConnectorFactory(info, redisConfig, redisImplementation, redisOptions, initializer ?? null);
            container.Register(c => new RedisHealthContributor(factory, redisImplementation, c.ResolveOptional<ILogger<RedisHealthContributor>>())).As<IHealthContributor>();
            return container.Register(c => factory.Create(null)).As(redisInterface, redisImplementation);
        }
    }
}
