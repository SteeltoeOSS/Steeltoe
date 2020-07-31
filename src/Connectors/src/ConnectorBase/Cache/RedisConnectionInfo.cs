// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.Redis
{
    public class RedisConnectionInfo : IConnectionInfo, IConnectionServiceInfo
    {
        public Connection Get(IConfiguration configuration, string serviceName)
        {
            var info = serviceName == null
                ? configuration.GetSingletonServiceInfo<RedisServiceInfo>()
                : configuration.GetRequiredServiceInfo<RedisServiceInfo>(serviceName);
            return GetConnection(info, configuration);
        }

        public Connection Get(IConfiguration configuration, IServiceInfo serviceInfo)
        {
            return GetConnection((RedisServiceInfo)serviceInfo, configuration);
        }

        private Connection GetConnection(RedisServiceInfo serviceInfo, IConfiguration configuration)
        {
            var redisConfig = new RedisCacheConnectorOptions(configuration);
            var configurer = new RedisCacheConfigurer();
            var connString = configurer.Configure(serviceInfo, redisConfig).ToString();
            return new Connection
            {
                ConnectionString = connString,
                Name = "Redis" + serviceInfo.Id?.Insert(0, "-")
            };
        }
    }
}