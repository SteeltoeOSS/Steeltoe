// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
