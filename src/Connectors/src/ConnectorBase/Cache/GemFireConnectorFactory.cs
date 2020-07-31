// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector.Services;
using System;

namespace Steeltoe.CloudFoundry.Connector.GemFire
{
    public class GemFireConnectorFactory
    {
        private readonly GemFireServiceInfo _info;
        private readonly ILogger<GemFireConnectorFactory> _logger;
        private readonly GemFireConnectorOptions _config;
        private readonly GemFireConfigurer _configurer = new GemFireConfigurer();

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
            var factory = ConnectorHelpers.CreateInstance(GemFireTypeLocator.CacheFactory);
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
                    authInitializerInstance = ConnectorHelpers.CreateInstance(authInitializer, new object[] { _config.Username, _config.Password });
                }
                catch (Exception e)
                {
                    _logger?.LogError("Failed to create an IAuthInitializer instance for GemFire!");
                    throw new ConnectorException("Failed to create an instance of Apache.Geode.Client.IAuthInitializer. Make sure you have a constructor that takes two strings (username and password) as parameters", e);
                }

                return GemFireTypeLocator.GetCacheAuthInitializer(authInitializer).Invoke(factory, new object[] { authInitializerInstance });
            }

            return factory;
        }

        public object CreateCache(object cacheFactory)
        {
            return GemFireTypeLocator.CacheInitializer.Invoke(cacheFactory, null);
        }

        public object CreatePoolFactory(object cache)
        {
            var poolFactory = GemFireTypeLocator.PoolFactoryInitializer.Invoke(cache, null);
            foreach (var locator in _config.ParsedLocators())
            {
                _logger?.LogTrace("Adding GemFire locator {GemFireLocator} to pool factory", locator.Key + ":" + locator.Value);
                GemFireTypeLocator.AddLocatorToPoolFactory.Invoke(poolFactory, new object[] { locator.Key, locator.Value });
            }

            return poolFactory;
        }
    }
}
