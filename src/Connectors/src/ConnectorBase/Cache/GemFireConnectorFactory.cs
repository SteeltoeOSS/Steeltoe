// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Reflection;
using Steeltoe.Connector.Services;
using System;

namespace Steeltoe.Connector.GemFire
{
    public class GemFireConnectorFactory
    {
        private readonly GemFireServiceInfo _info;
        private readonly ILogger<GemFireConnectorFactory> _logger;
        private GemFireConnectorOptions _config;
        private GemFireConfigurer _configurer = new GemFireConfigurer();

        /// <summary>
        /// Initializes a new instance of the <see cref="GemFireConnectorFactory"/> class.
        /// Factory for creating connections and objects to use with GemFire
        /// </summary>
        /// <param name="sinfo">Service Binding Info</param>
        /// <param name="logger">For logging within the ConnectorFactory</param>
        /// <param name="config">GemFire client configuration</param>
        public GemFireConnectorFactory(GemFireConnectorOptions config, GemFireServiceInfo sinfo, ILogger<GemFireConnectorFactory> logger = null)
        {
            _info = sinfo;
            _config = _configurer.Configure(sinfo, config) ?? throw new ArgumentNullException(nameof(config));
            _logger = logger;
        }

        public object CreateCacheFactory(IServiceProvider provider, Type authInitializer)
        {
            var factory = ReflectionHelpers.CreateInstance(GemFireTypeLocator.CacheFactory);
            foreach (var prop in _config.Properties)
            {
                GemFireTypeLocator.CachePropertySetter.Invoke(factory, new object[] { prop.Key, prop.Value });
                _logger?.LogDebug("Setting GemFire property {GemFireProperty}", prop.Key);
            }

            if (!string.IsNullOrEmpty(_config.Username) && !string.IsNullOrEmpty(_config.Password))
            {
                _logger?.LogDebug("GemFire credentials found");
                object authInitializerInstance;
                try
                {
                    authInitializerInstance = ReflectionHelpers.CreateInstance(authInitializer, new object[] { _config.Username, _config.Password });
                }
                catch (Exception e)
                {
                    _logger?.LogError("Failed to create an IAuthInitializer instance for GemFire!");
                    throw new ConnectorException("Failed to create an instance of Apache.Geode.Client.IAuthInitializer. Make sure you have a constructor that takes two strings (username and password) as parameters", e);
                }

                GemFireTypeLocator.GetCacheAuthInitializer(authInitializer).SetValue(factory, authInitializerInstance, null);
                return factory;
            }

            return factory;
        }

        public object CreateCache(object cacheFactory)
        {
            return GemFireTypeLocator.CacheInitializer.Invoke(cacheFactory, null);
        }

        public object CreatePoolFactory(object cache)
        {
            var poolFactory = GemFireTypeLocator.GetPoolFactoryInitializer.GetValue(cache);
            foreach (var locator in _config.ParsedLocators())
            {
                _logger?.LogTrace("Adding GemFire locator {GemFireLocator} to pool factory", locator.Key + ":" + locator.Value);
                GemFireTypeLocator.AddLocatorToPoolFactory.Invoke(poolFactory, new object[] { locator.Key, locator.Value });
            }

            return poolFactory;
        }
    }
}
