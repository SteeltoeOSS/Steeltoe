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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector.Services;
using System;
using System.Reflection;
using System.IO;

namespace Steeltoe.CloudFoundry.Connector.Redis
{
    public static class RedisCacheServiceCollectionExtensions
    {
        #region Microsoft.Extensions.Caching.Redis

        /// <summary>
        /// Add IDistributedCache to ServiceCollection
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config"></param>
        /// <param name="logFactory"></param>
        /// <returns></returns>
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
        /// Add IDistributedCache to ServiceCollection
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config"></param>
        /// <param name="serviceName"></param>
        /// <param name="logFactory"></param>
        /// <returns></returns>
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
        /// Add IDistributedCache to ServiceCollection
        /// </summary>
        /// <param name="services"></param>
        /// <param name="applicationConfiguration"></param>
        /// <param name="connectorConfiguration"></param>
        /// <param name="serviceName"></param>
        /// <param name="contextLifetime"></param>
        /// <returns></returns>
        public static IServiceCollection AddDistributedRedisCache(this IServiceCollection services, IConfiguration applicationConfiguration, IConfiguration connectorConfiguration, string serviceName, ServiceLifetime contextLifetime = ServiceLifetime.Scoped)
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
            RedisServiceInfo info;
            if (serviceName != null)
            {
                info = configToConfigure.GetRequiredServiceInfo<RedisServiceInfo>(serviceName);
            }
            else
            {
                info = configToConfigure.GetSingletonServiceInfo<RedisServiceInfo>();
            }

            DoAddIDistributedCache(services, info, configToConfigure, contextLifetime);
            return services;
        }

        #endregion

        #region StackExchange.Redis

        /// <summary>
        /// Add Redis Connection Multiplexer to ServiceCollection
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config"></param>
        /// <returns></returns>
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
        /// Add Redis Connection Multiplexer to ServiceCollection
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config"></param>
        /// <param name="serviceName"></param>
        /// <returns></returns>
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
        /// Add Redis Connection Multiplexer to ServiceCollection
        /// </summary>
        /// <param name="services"></param>
        /// <param name="applicationConfiguration"></param>
        /// <param name="connectorConfiguration"></param>
        /// <param name="serviceName"></param>
        /// <param name="contextLifetime"></param>
        /// <returns></returns>
        public static IServiceCollection AddRedisConnectionMultiplexer(this IServiceCollection services, IConfiguration applicationConfiguration, IConfiguration connectorConfiguration, string serviceName, ServiceLifetime contextLifetime = ServiceLifetime.Scoped)
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
            // RedisServiceInfo info = configToConfigure.GetRequiredServiceInfo<RedisServiceInfo>(serviceName);
            RedisServiceInfo info = serviceName == null ? configToConfigure.GetSingletonServiceInfo<RedisServiceInfo>() : configToConfigure.GetRequiredServiceInfo<RedisServiceInfo>(serviceName);
            DoAddConnectionMultiplexer(services, info, configToConfigure, contextLifetime);
            return services;
        }

        #endregion

        private static void DoAddIDistributedCache(IServiceCollection services, RedisServiceInfo info, IConfiguration config, ServiceLifetime contextLifetime)
        {
            string[] redisAssemblies = new string[] { "Microsoft.Extensions.Caching.Redis" };
            string[] redisTypeNames = new string[] { "Microsoft.Extensions.Caching.Redis.RedisCache" };
            string[] redisOptionNames = new string[] { "Microsoft.Extensions.Caching.Redis.RedisCacheOptions" };

            Type redisConnection = ConnectorHelpers.FindType(redisAssemblies, redisTypeNames);
            Type redisOptions = ConnectorHelpers.FindType(redisAssemblies, redisOptionNames);

            if (redisConnection == null || redisOptions == null)
            {
                throw new ConnectorException("Unable to find required Redis types, are you missing the Microsoft.Extensions.Caching.Redis Nuget package?");
            }

            RedisCacheConnectorOptions redisConfig = new RedisCacheConnectorOptions(config);
            RedisServiceConnectorFactory factory = new RedisServiceConnectorFactory(info, redisConfig, redisConnection, redisOptions, null);
            services.Add(new ServiceDescriptor(redisConnection, factory.Create, contextLifetime));
        }

        private static void DoAddConnectionMultiplexer(IServiceCollection services, RedisServiceInfo info, IConfiguration config, ServiceLifetime contextLifetime)
        {
            string[] redisAssemblies = new string[] { "StackExchange.Redis", "StackExchange.Redis.StrongName" };
            string[] redisTypeNames = new string[] { "StackExchange.Redis.ConnectionMultiplexer" };
            string[] redisOptionNames = new string[] { "StackExchange.Redis.ConfigurationOptions" };

            Type redisConnection = ConnectorHelpers.FindType(redisAssemblies, redisTypeNames);
            Type redisOptions = ConnectorHelpers.FindType(redisAssemblies, redisOptionNames);
            MethodInfo initializer = ConnectorHelpers.FindMethod(redisConnection, "Connect", new Type[] { redisOptions, typeof(TextWriter) });

            if (redisConnection == null || redisOptions == null || initializer == null)
            {
                throw new ConnectorException("Unable to find required Redis types, are you missing a StackExchange.Redis Nuget Package?");
            }

            RedisCacheConnectorOptions redisConfig = new RedisCacheConnectorOptions(config);
            RedisServiceConnectorFactory factory = new RedisServiceConnectorFactory(info, redisConfig, redisConnection, redisOptions, initializer != null ? initializer : null);
            services.Add(new ServiceDescriptor(redisConnection, factory.Create, contextLifetime));
        }
    }
}
