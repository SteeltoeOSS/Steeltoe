// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Reflection;
using Steeltoe.Connector.Services;
using System;
using System.Reflection;

namespace Steeltoe.Connector.Redis
{
    public class RedisServiceConnectorFactory
    {
        private RedisServiceInfo _info;
        private RedisCacheConnectorOptions _config;
        private RedisCacheConfigurer _configurer = new RedisCacheConfigurer();

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisServiceConnectorFactory"/> class.
        /// Factory for creating Redis connections with either Microsoft.Extensions.Caching.Redis or StackExchange.Redis
        /// </summary>
        /// <param name="sinfo">Service Info</param>
        /// <param name="config">Service Configuration</param>
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

        /// <summary>
        /// Get the connection string from Configuration sources
        /// </summary>
        /// <returns>Connection String</returns>
        public string GetConnectionString()
        {
            var connectionOptions = _configurer.Configure(_info, _config);
            return connectionOptions.ToString();
        }

        protected Type ConnectorType { get; set; }

        protected Type OptionsType { get; set; }

        protected MethodInfo Initializer { get; set; }

        /// <summary>
        /// Open the Redis connection
        /// </summary>
        /// <param name="provider">IServiceProvider</param>
        /// <returns>Initialized Redis connection</returns>
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
            return ReflectionHelpers.CreateInstance(ConnectorType, new object[] { options });
        }

        private object CreateConnectionByMethod(object options)
        {
            return Initializer.Invoke(ConnectorType, new object[] { options, null });
        }
    }
}
