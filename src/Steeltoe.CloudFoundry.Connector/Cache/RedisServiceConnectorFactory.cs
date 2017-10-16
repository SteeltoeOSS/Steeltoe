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
using System;
using System.Reflection;

namespace Steeltoe.CloudFoundry.Connector.Redis
{
    public class RedisServiceConnectorFactory
    {
        private RedisServiceInfo _info;
        private RedisCacheConnectorOptions _config;
        private RedisCacheConfigurer _configurer = new RedisCacheConfigurer();

        /// <summary>
        /// Factory for creating Redis connections with either Microsoft.Extensions.Caching.Redis or StackExchange.Redis
        /// </summary>
        /// <param name="sinfo"></param>
        /// <param name="config"></param>
        /// <param name="connectionType">Redis connection Type</param>
        /// <param name="optionsType">Options Type used to establish connection</param>
        /// <param name="initalizer">Method used to open connection</param>
        public RedisServiceConnectorFactory(RedisServiceInfo sinfo, RedisCacheConnectorOptions config, Type connectionType, Type optionsType, MethodInfo initalizer)
        {
            _info = sinfo;
            _config = config ?? throw new ArgumentNullException(nameof(config), "Cache connector options must be provided");
            ConnectorType = connectionType;
            OptionsType = optionsType;
            Initializer = initalizer;
        }

        protected Type ConnectorType { get; set; }

        protected Type OptionsType { get; set; }

        protected MethodInfo Initializer { get; set; }

        /// <summary>
        /// Open the Redis connection
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        public virtual object Create(IServiceProvider provider)
        {
            var connectionOptions = _configurer.Configure(_info, _config);

            object result = null;
            if (Initializer == null)
            {
                result = CreateConnection(connectionOptions.ToMicrosoftExtensionObject(OptionsType));
            }
            else
            { 
                result = CreateConnectionByMethod(connectionOptions.ToStackExchangeObject(OptionsType));
            }

            return result;
        }

        private object CreateConnection(object options)
        {
            return ConnectorHelpers.CreateInstance(ConnectorType, new object[] { options });
        }

        private object CreateConnectionByMethod(object options)
        {
            return Initializer.Invoke(ConnectorType, new object[] { options, null });
        }
    }
}
