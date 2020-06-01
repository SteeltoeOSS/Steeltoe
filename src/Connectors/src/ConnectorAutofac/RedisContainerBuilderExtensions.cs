// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
