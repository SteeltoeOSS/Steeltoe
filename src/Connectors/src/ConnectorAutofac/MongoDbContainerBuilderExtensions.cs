// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Autofac.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector;
using Steeltoe.CloudFoundry.Connector.MongoDb;
using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.Common.HealthChecks;
using System;

namespace Steeltoe.CloudFoundry.ConnectorAutofac
{
    public static class MongoDbContainerBuilderExtensions
    {
        /// <summary>
        /// Adds MongoDb classes (MongoClient, IMongoClient and MongoUrl) to your Autofac Container
        /// </summary>
        /// <param name="container">Your Autofac Container Builder</param>
        /// <param name="config">Application configuration</param>
        /// <param name="serviceName">Cloud Foundry service name binding</param>
        /// <returns>the RegistrationBuilder for (optional) additional configuration</returns>
        public static IRegistrationBuilder<object, SimpleActivatorData, SingleRegistrationStyle> RegisterMongoDbConnection(this ContainerBuilder container, IConfiguration config, string serviceName = null)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var info = serviceName == null
                ? config.GetSingletonServiceInfo<MongoDbServiceInfo>()
                : config.GetRequiredServiceInfo<MongoDbServiceInfo>(serviceName);

            var mongoOptions = new MongoDbConnectorOptions(config);
            var clientFactory = new MongoDbConnectorFactory(info, mongoOptions, MongoDbTypeLocator.MongoClient);
            var urlFactory = new MongoDbConnectorFactory(info, mongoOptions, MongoDbTypeLocator.MongoUrl);

            container.Register(c => urlFactory.Create(null)).As(MongoDbTypeLocator.MongoUrl);
            container.Register(c => new MongoDbHealthContributor(clientFactory, c.ResolveOptional<ILogger<MongoDbHealthContributor>>())).As<IHealthContributor>();

            return container.Register(c => clientFactory.Create(null)).As(MongoDbTypeLocator.IMongoClient, MongoDbTypeLocator.MongoClient);
        }
    }
}
