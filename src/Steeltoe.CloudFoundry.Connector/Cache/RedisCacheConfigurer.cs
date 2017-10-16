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

using Steeltoe.CloudFoundry.Connector.Services;

namespace Steeltoe.CloudFoundry.Connector.Redis
{
    public class RedisCacheConfigurer
    {
        /// <summary>
        /// Create a configuration object to be used to connect to Redis
        /// </summary>
        /// <param name="si"></param>
        /// <param name="configuration">Configuration parameters</param>
        /// <returns>A dynamically typed object for use connecting to Redis</returns>
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
        }
    }
}
