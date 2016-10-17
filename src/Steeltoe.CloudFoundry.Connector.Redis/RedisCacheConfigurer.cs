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

using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Steeltoe.CloudFoundry.Connector.Services;


namespace Steeltoe.CloudFoundry.Connector.Redis
{
    public class RedisCacheConfigurer
    {

        internal IOptions<RedisCacheOptions> Configure(RedisServiceInfo si, RedisCacheConnectorOptions options)
        {
            UpdateOptions(si, options);
            
            RedisCacheOptions redisOptions = new RedisCacheOptions();
            UpdateOptions(options, redisOptions);

            return new ConnectorIOptions<RedisCacheOptions>(redisOptions);

        }

        internal void UpdateOptions(RedisCacheConnectorOptions options, RedisCacheOptions redisOptions )
        {
            redisOptions.Configuration = options.ToString();
            redisOptions.InstanceName = options.InstanceId;
        }

        internal void UpdateOptions(RedisServiceInfo si, RedisCacheConnectorOptions options)
        {
            if (si == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(si.Host))
            {
                options.Host = si.Host;
                options.Port = si.Port;
                options.EndPoints = null;
            }

            if (!string.IsNullOrEmpty(si.Password))
            {
                options.Password = si.Password;
            }

            if (!string.IsNullOrEmpty(si.ApplicationInfo.InstanceId))
            {
                options.InstanceId = si.ApplicationInfo.InstanceId;
            }
        }

        internal ConfigurationOptions ConfigureConnection(RedisServiceInfo si, RedisCacheConnectorOptions options)
        {
            UpdateOptions(si, options);
            ConfigurationOptions redisOptions = ConfigurationOptions.Parse(options.ToString());
            return redisOptions;

        }
    }
}


