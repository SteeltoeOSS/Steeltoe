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
using Steeltoe.Common.HealthChecks;
using System;
using System.Reflection;

namespace Steeltoe.CloudFoundry.Connector.Redis
{
    public static class RedisCacheServiceCollectionExtensions
    {
        #region Microsoft.Extensions.Caching.Redis

        /// <summary>
        /// Add IDistributedCache and its IHealthContributor to ServiceCollection
        /// </summary>
        /// <param name="services">Service collection to add to</param>
        /// <param name="config">App configuration</param>
        /// <param name="logFactory">logger factory</param>
        /// <returns>IServiceCollection for chaining</returns>
        /// <remarks>RedisCache is retrievable as both RedisCache and IDistributedCache</remarks>
        public static IServiceCollection AddDistributedRedisCache(this IServiceCollection services, IConfiguration config, ILoggerFactory logFactory = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return services.AddDistributedRedisCache(config, config, null);
        }

        /// <summary>
        /// Add IDistributedCache and its IHealthContributor to ServiceCollection
        /// </summary>
        /// <param name="services">Service collection to add to</param>
        /// <param name="config">App configuration</param>
        /// <param name="serviceName">Name of service to add</param>
        /// <param name="logFactory">logger factory</param>
        /// <returns>IServiceCollection for chaining</returns>
        /// <remarks>RedisCache is retrievable as both RedisCache and IDistributedCache</remarks>
        public static IServiceCollection AddDistributedRedisCache(this IServiceCollection services, IConfiguration config, string serviceName, ILoggerFactory logFactory = null)
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

            return services.AddDistributedRedisCache(config, config, serviceName);
        }

        /// <summary>
        /// Add IDistributedCache and its IHealthContributor to ServiceCollection
        /// </summary>
        /// <param name="services">Service collection to add to</param>
        /// <param name="applicationConfiguration">App configuration</param>
        /// <param name="connectorConfiguration">Connector configuration</param>
        /// <param name="serviceName">Name of service to add</param>
        /// <param name="contextLifetime"><see cref="ServiceLifetime"/> of the service to inject</param>
        /// <returns>IServiceCollection for chaining</returns>
        /// <remarks>RedisCache is retrievable as both RedisCache and IDistributedCache</remarks>
        public static IServiceCollection AddDistributedRedisCache(this IServiceCollection services, IConfiguration applicationConfiguration, IConfiguration connectorConfiguration, string serviceName, ServiceLifetime contextLifetime = ServiceLifetime.Singleton)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (applicationConfiguration == null)
            {
                throw new ArgumentNullException(nameof(applicationConfiguration));
            }

            var configToConfigure = connectorConfiguration ?? applicationConfiguration;

            RedisServiceInfo info = serviceName != null
                ? configToConfigure.GetRequiredServiceInfo<RedisServiceInfo>(serviceName)
                : configToConfigure.GetSingletonServiceInfo<RedisServiceInfo>();

            DoAddIDistributedCache(services, info, configToConfigure, contextLifetime);
            return services;
        }

        #endregion

        #region StackExchange.Redis

        /// <summary>
        /// Add Redis Connection Multiplexer and its IHealthContributor to ServiceCollection
        /// </summary>
        /// <param name="services">Service collection to add to</param>
        /// <param name="config">App configuration</param>
        /// <returns>IServiceCollection for chaining</returns>
        /// <remarks>ConnectionMultiplexer is retrievable as both ConnectionMultiplexer and IConnectionMultiplexer</remarks>
        public static IServiceCollection AddRedisConnectionMultiplexer(this IServiceCollection services, IConfiguration config)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return services.AddRedisConnectionMultiplexer(config, config, null);
        }

        /// <summary>
        /// Add Redis Connection Multiplexer and its IHealthContributor to ServiceCollection
        /// </summary>
        /// <param name="services">Service collection to add to</param>
        /// <param name="config">App configuration</param>
        /// <param name="serviceName">Name of service to add</param>
        /// <returns>IServiceCollection for chaining</returns>
        /// <remarks>ConnectionMultiplexer is retrievable as both ConnectionMultiplexer and IConnectionMultiplexer</remarks>
        public static IServiceCollection AddRedisConnectionMultiplexer(this IServiceCollection services, IConfiguration config, string serviceName)
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

            return services.AddRedisConnectionMultiplexer(config, config, serviceName);
        }

        /// <summary>
        /// Add Redis Connection Multiplexer and its IHealthContributor to ServiceCollection
        /// </summary>
        /// <param name="services">Service collection to add to</param>
        /// <param name="applicationConfiguration">App configuration</param>
        /// <param name="connectorConfiguration">Connector configuration</param>
        /// <param name="serviceName">Name of service to add</param>
        /// <param name="contextLifetime"><see cref="ServiceLifetime"/> of the service to inject</param>
        /// <returns>IServiceCollection for chaining</returns>
        /// <remarks>ConnectionMultiplexer is retrievable as both ConnectionMultiplexer and IConnectionMultiplexer</remarks>
        public static IServiceCollection AddRedisConnectionMultiplexer(this IServiceCollection services, IConfiguration applicationConfiguration, IConfiguration connectorConfiguration, string serviceName, ServiceLifetime contextLifetime = ServiceLifetime.Singleton)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (applicationConfiguration == null)
            {
                throw new ArgumentNullException(nameof(applicationConfiguration));
            }

            var configToConfigure = connectorConfiguration ?? applicationConfiguration;
            RedisServiceInfo info = serviceName == null ? configToConfigure.GetSingletonServiceInfo<RedisServiceInfo>() : configToConfigure.GetRequiredServiceInfo<RedisServiceInfo>(serviceName);
            DoAddConnectionMultiplexer(services, info, configToConfigure, contextLifetime);
            return services;
        }

        #endregion

        private static void DoAddIDistributedCache(IServiceCollection services, RedisServiceInfo info, IConfiguration config, ServiceLifetime contextLifetime)
        {
            Type interfaceType = RedisTypeLocator.MicrosoftInterface;
            Type connectionType = RedisTypeLocator.MicrosoftImplementation;
            Type optionsType = RedisTypeLocator.MicrosoftOptions;

            RedisCacheConnectorOptions redisConfig = new RedisCacheConnectorOptions(config);
            RedisServiceConnectorFactory factory = new RedisServiceConnectorFactory(info, redisConfig, connectionType, optionsType, null);
            services.Add(new ServiceDescriptor(interfaceType, factory.Create, contextLifetime));
            services.Add(new ServiceDescriptor(connectionType, factory.Create, contextLifetime));
            services.Add(new ServiceDescriptor(typeof(IHealthContributor), ctx => new RedisHealthContributor(factory, connectionType, ctx.GetService<ILogger<RedisHealthContributor>>()), ServiceLifetime.Singleton));
        }

        private static void DoAddConnectionMultiplexer(IServiceCollection services, RedisServiceInfo info, IConfiguration config, ServiceLifetime contextLifetime)
        {
            Type redisInterface = RedisTypeLocator.StackExchangeInterface;
            Type redisImplementation = RedisTypeLocator.StackExchangeImplementation;
            Type redisOptions = RedisTypeLocator.StackExchangeOptions;
            MethodInfo initializer = RedisTypeLocator.StackExchangeInitializer;

            RedisCacheConnectorOptions redisConfig = new RedisCacheConnectorOptions(config);
            RedisServiceConnectorFactory factory = new RedisServiceConnectorFactory(info, redisConfig, redisImplementation, redisOptions, initializer ?? null);
            services.Add(new ServiceDescriptor(redisInterface, factory.Create, contextLifetime));
            services.Add(new ServiceDescriptor(redisImplementation, factory.Create, contextLifetime));
            services.Add(new ServiceDescriptor(typeof(IHealthContributor), ctx => new RedisHealthContributor(factory, redisImplementation, ctx.GetService<ILogger<RedisHealthContributor>>()), ServiceLifetime.Singleton));
        }
    }
}
