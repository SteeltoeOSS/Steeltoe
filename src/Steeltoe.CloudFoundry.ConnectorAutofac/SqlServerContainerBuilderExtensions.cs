// Copyright 2017 the original author or authors.
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

using Autofac;
using Autofac.Builder;
using Microsoft.Extensions.Configuration;
using Steeltoe.CloudFoundry.Connector;
using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.CloudFoundry.Connector.SqlServer;
using System;
using System.Data;
using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.ConnectorBase.Cache;
using Steeltoe.CloudFoundry.ConnectorBase.Relational;
using Steeltoe.Management.Endpoint.Health;

namespace Steeltoe.CloudFoundry.ConnectorAutofac
{
    public static class SqlServerContainerBuilderExtensions
    {
        private static string[] sqlServerAssemblies = new string[] { "System.Data.SqlClient" };
        private static string[] sqlServerTypeNames = new string[] { "System.Data.SqlClient.SqlConnection" };

        /// <summary>
        /// Adds SqlConnection (as IDbConnection) to your Autofac Container
        /// </summary>
        /// <param name="container">Your Autofac Container Builder</param>
        /// <param name="config">Application configuration</param>
        /// <param name="serviceName">Cloud Foundry service name binding</param>
        /// <returns>the RegistrationBuilder for (optional) additional configuration</returns>
        public static IRegistrationBuilder<object, SimpleActivatorData, SingleRegistrationStyle> RegisterSqlServerConnection(this ContainerBuilder container, IConfiguration config, string serviceName = null)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            Type sqlServerConnection = ConnectorHelpers.FindType(sqlServerAssemblies, sqlServerTypeNames);
            if (sqlServerConnection == null)
            {
                throw new ConnectorException("Unable to find System.Data.SqlClient.SqlConnection, are you missing a System.Data.SqlClient reference?");
            }

            SqlServerServiceInfo info;
            if (serviceName == null)
            {
                info = config.GetSingletonServiceInfo<SqlServerServiceInfo>();
            }
            else
            {
                info = config.GetRequiredServiceInfo<SqlServerServiceInfo>(serviceName);
            }

            var sqlServerConfig = new SqlServerProviderConnectorOptions(config);
            var factory = new SqlServerProviderConnectorFactory(info, sqlServerConfig, sqlServerConnection);
            container.RegisterType<RelationalHealthContributor>().As<IHealthContributor>();
            return container.Register(c => factory.Create(null)).As<IDbConnection>();
        }
    }
}
