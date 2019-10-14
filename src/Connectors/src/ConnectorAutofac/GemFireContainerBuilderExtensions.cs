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

using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector;
using Steeltoe.CloudFoundry.Connector.GemFire;
using Steeltoe.CloudFoundry.Connector.Services;
using System;

namespace Steeltoe.CloudFoundry.ConnectorAutofac
{
    public static class GemFireContainerBuilderExtensions
    {
        /// <summary>
        /// Add GemFire to a <see cref="ContainerBuilder"/>
        /// </summary>
        /// <param name="container">Your <see cref="ContainerBuilder"/></param>
        /// <param name="config">Application Configuration</param>
        /// <param name="authInitializer">Your class that implements Apache.Geode.Client.IAuthInitializer</param>
        /// <param name="serviceName">Cloud Foundry service binding's name</param>
        /// <param name="loggerFactory">Logger factory</param>
        public static void RegisterGemFireConnection(this ContainerBuilder container, IConfiguration config, Type authInitializer, string serviceName = null, ILoggerFactory loggerFactory = null)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            GemFireServiceInfo info = serviceName == null
                ? config.GetSingletonServiceInfo<GemFireServiceInfo>()
                : config.GetRequiredServiceInfo<GemFireServiceInfo>(serviceName);

            Type cacheFactory = GemFireTypeLocator.CacheFactory;
            Type cache = GemFireTypeLocator.Cache;
            Type poolFactory = GemFireTypeLocator.PoolFactory;

            //// Type regionFactory = GemFireTypeLocator.RegionFactory;

            var gemFireConfig = new GemFireConnectorOptions(config);
            var connectorFactory = new GemFireConnectorFactory(gemFireConfig, info);

            // TODO: health check
            //// container.RegisterType<RelationalHealthContributor>().As<IHealthContributor>();

            container.Register(c => connectorFactory.CreateCacheFactory(null, authInitializer)).As(cacheFactory).SingleInstance().AutoActivate();
            container.Register(c => connectorFactory.CreateCache(c.Resolve(cacheFactory))).As(cache).SingleInstance();
            container.Register(c => connectorFactory.CreatePoolFactory(c.Resolve(cache))).As(poolFactory).SingleInstance();
        }
    }
}
