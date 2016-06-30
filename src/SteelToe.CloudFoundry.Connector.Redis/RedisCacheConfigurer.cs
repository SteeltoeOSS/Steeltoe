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
using SteelToe.CloudFoundry.Connector.Services;


namespace SteelToe.CloudFoundry.Connector.Redis
{
    public class RedisCacheConfigurer
    {
    
        internal IOptions<RedisCacheOptions> Configure(RedisServiceInfo si, RedisCacheConnectorOptions configuration)
        {
            RedisCacheOptions redisOptions = new RedisCacheOptions();
            UpdateOptions(configuration, redisOptions);
            UpdateOptions(si, redisOptions);
            return new ConnectorIOptions<RedisCacheOptions>(redisOptions);
            
        }
        internal void UpdateOptions(RedisServiceInfo si, RedisCacheOptions options)
        {
            if (si == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(si.Host))
            {
                string msConfiguration = si.Host + ":" + si.Port;
                if (!string.IsNullOrEmpty(si.Password))
                {
                    msConfiguration = msConfiguration + ",password=" + si.Password;
                }

                options.Configuration = msConfiguration;
                options.InstanceName = si.ApplicationInfo.InstanceId;
            }
        }

        internal void UpdateOptions(RedisCacheConnectorOptions config, RedisCacheOptions options)
        {
            if (config == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(config.ConnectionString))
            {
                options.Configuration = config.ConnectionString;
                options.InstanceName = config.InstanceId;
            }
            else
            {
                string msConfiguration = config.Host + ":" + config.Port;
                if (!string.IsNullOrEmpty(config.Password))
                {
                    msConfiguration = msConfiguration + ",password=" + config.Password;
                }

                options.Configuration = msConfiguration;
                options.InstanceName = config.InstanceId;

            }

            return;
        }
    }

}
