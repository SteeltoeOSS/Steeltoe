// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Autofac.Builder;
using Microsoft.Extensions.Configuration;
using Steeltoe.CloudFoundry.Connector;
using Steeltoe.CloudFoundry.Connector.PostgreSql;
using Steeltoe.CloudFoundry.Connector.Relational;
using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.Common.HealthChecks;
using System;
using System.Data;

namespace Steeltoe.CloudFoundry.ConnectorAutofac
{
    public static class PostgreSqlContainerBuilderExtensions
    {
        /// <summary>
        /// Adds NpgsqlConnection (as IDbConnection and NpgsqlConnection) to your Autofac Container
        /// </summary>
        /// <param name="container">Your Autofac Container Builder</param>
        /// <param name="config">Application configuration</param>
        /// <param name="serviceName">Cloud Foundry service name binding</param>
        /// <returns>the RegistrationBuilder for (optional) additional configuration</returns>
        public static IRegistrationBuilder<object, SimpleActivatorData, SingleRegistrationStyle> RegisterPostgreSqlConnection(this ContainerBuilder container, IConfiguration config, string serviceName = null)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            PostgresServiceInfo info = serviceName == null
                ? config.GetSingletonServiceInfo<PostgresServiceInfo>()
                : config.GetRequiredServiceInfo<PostgresServiceInfo>(serviceName);

            Type postgreSqlConnection = PostgreSqlTypeLocator.NpgsqlConnection;
            var postgreSqlConfig = new PostgresProviderConnectorOptions(config);
            var factory = new PostgresProviderConnectorFactory(info, postgreSqlConfig, postgreSqlConnection);
            container.RegisterType<RelationalHealthContributor>().As<IHealthContributor>();
            return container.Register(c => factory.Create(null)).As(typeof(IDbConnection), postgreSqlConnection);
        }
    }
}
