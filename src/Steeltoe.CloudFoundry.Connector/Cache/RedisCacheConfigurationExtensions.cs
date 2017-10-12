using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Steeltoe.CloudFoundry.Connector.Services;

namespace Steeltoe.CloudFoundry.Connector.Redis
{
    public static class RedisCacheConfigurationExtensions
    {
        public static RedisServiceConnectorFactory CreateRedisServiceConnectorFactory(this IConfiguration config, string serviceName = null)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var redisConfig = new RedisCacheConnectorOptions(config);
            return config.CreateRedisServiceConnectorFactory(config, serviceName);
        }

        public static RedisServiceConnectorFactory CreateRedisServiceConnectorFactory(this IConfiguration config, IConfiguration connectorConfiguration, string serviceName = null)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (connectorConfiguration == null)
            {
                throw new ArgumentNullException(nameof(connectorConfiguration));
            }

            var connectorOptions = new RedisCacheConnectorOptions(connectorConfiguration);
            return config.CreateRedisServiceConnectorFactory(connectorOptions, serviceName);
        }

        public static RedisServiceConnectorFactory CreateRedisServiceConnectorFactory(this IConfiguration config, RedisCacheConnectorOptions connectorOptions, string serviceName = null)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (connectorOptions == null)
            {
                throw new ArgumentNullException(nameof(connectorOptions));
            }

            var info = serviceName == null ? config.GetSingletonServiceInfo<RedisServiceInfo>() : config.GetRequiredServiceInfo<RedisServiceInfo>(serviceName);
            return new RedisServiceConnectorFactory(info, connectorOptions);
        }
    }
}
