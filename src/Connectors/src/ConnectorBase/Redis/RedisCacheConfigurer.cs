// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.Redis;

public class RedisCacheConfigurer
{
    /// <summary>
    /// Create a configuration object to be used to connect to Redis.
    /// </summary>
    /// <param name="si">Redis Service Info.</param>
    /// <param name="configuration">Configuration parameters.</param>
    /// <returns>A dynamically typed object for use connecting to Redis.</returns>
    public RedisCacheConnectorOptions Configure(RedisServiceInfo si, RedisCacheConnectorOptions configuration)
    {
        // apply service info to exising configuration
        UpdateOptions(si, configuration);
        return configuration;
    }

    internal void UpdateOptions(RedisServiceInfo si, RedisCacheConnectorOptions configuration)
    {
        if (si == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(si.Host))
        {
            configuration.Host = si.Host;
            configuration.Port = si.Port;
            configuration.EndPoints = null;
        }

        if (!string.IsNullOrEmpty(si.Password))
        {
            configuration.Password = si.Password;
        }

        if (si.Scheme == RedisServiceInfo.REDIS_SECURE_SCHEME)
        {
            configuration.Ssl = true;
        }
    }
}
