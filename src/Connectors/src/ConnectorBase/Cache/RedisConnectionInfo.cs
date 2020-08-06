// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.Services;
using System;

namespace Steeltoe.Connector.Redis
{
    public class RedisConnectionInfo : IConnectionInfo
    {
        public Connection Get(IConfiguration configuration, string serviceName)
        {
            var info = string.IsNullOrEmpty(serviceName)
                ? configuration.GetSingletonServiceInfo<RedisServiceInfo>()
                : configuration.GetRequiredServiceInfo<RedisServiceInfo>(serviceName);
            return GetConnection(info, configuration);
        }

        public Connection Get(IConfiguration configuration, IServiceInfo serviceInfo)
            => GetConnection((RedisServiceInfo)serviceInfo, configuration);

        public bool IsSameType(string serviceType)
            => serviceType.Equals("redis", StringComparison.InvariantCultureIgnoreCase);

        public bool IsSameType(IServiceInfo serviceInfo)
            => serviceInfo is RedisServiceInfo;

        private Connection GetConnection(RedisServiceInfo serviceInfo, IConfiguration configuration)
        {
            var redisConfig = new RedisCacheConnectorOptions(configuration);
            var configurer = new RedisCacheConfigurer();
            return new Connection(configurer.Configure(serviceInfo, redisConfig).ToString(), "Redis", serviceInfo);
        }
    }
}