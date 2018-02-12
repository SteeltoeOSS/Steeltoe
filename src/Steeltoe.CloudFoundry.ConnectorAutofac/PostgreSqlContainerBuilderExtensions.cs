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
using Steeltoe.CloudFoundry.Connector.PostgreSql;
using Steeltoe.CloudFoundry.Connector.Services;
using System;
using System.Data;

namespace Steeltoe.CloudFoundry.ConnectorAutofac
{
    public static class PostgreSqlContainerBuilderExtensions
    {
        private static string[] postgreSqlAssemblies = new string[] { "Npgsql" };
        private static string[] postgreSqlTypeNames = new string[] { "Npgsql.NpgsqlConnection" };

        public static IRegistrationBuilder<object, SimpleActivatorData, SingleRegistrationStyle> RegisterPostgreSqlConnection(this ContainerBuilder container, IConfiguration config)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            Type postgreSqlConnection = ConnectorHelpers.FindType(postgreSqlAssemblies, postgreSqlTypeNames);
            if (postgreSqlConnection == null)
            {
                throw new ConnectorException("Unable to find NpgsqlConnection, are you missing a reference to Npgsql?");
            }

            var postgreSqlConfig = new PostgresProviderConnectorOptions(config);
            PostgresServiceInfo info = config.GetSingletonServiceInfo<PostgresServiceInfo>();
            PostgresProviderConnectorFactory factory = new PostgresProviderConnectorFactory(info, postgreSqlConfig, postgreSqlConnection);
            return container.Register(c => factory.Create(null)).As<IDbConnection>();
        }
    }
}
