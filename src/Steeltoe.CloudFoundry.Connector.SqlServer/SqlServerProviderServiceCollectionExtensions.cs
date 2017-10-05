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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector.Services;
using System;

namespace Steeltoe.CloudFoundry.Connector.SqlServer
{
    public static class SqlServerProviderServiceCollectionExtensions
    {
        public static IServiceCollection AddSqlServerConnection(this IServiceCollection services, IConfiguration config, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ILoggerFactory logFactory = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            SqlServerServiceInfo info = config.GetSingletonServiceInfo<SqlServerServiceInfo>();
            DoAdd(services, info, config, contextLifetime);

            return services;
        }

        public static IServiceCollection AddSqlServerConnection(this IServiceCollection services, IConfiguration config, string serviceName, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ILoggerFactory logFactory = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentNullException(nameof(serviceName));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            SqlServerServiceInfo info = config.GetRequiredServiceInfo<SqlServerServiceInfo>(serviceName);
            DoAdd(services, info, config, contextLifetime);

            return services;
        }

        private static string[] SqlServerAssemblies = new string[] { "System.Data.SqlClient" };

        private static string[] SqlServerTypeNames = new string[] { "System.Data.SqlClient.SqlConnection" };

        private static void DoAdd(IServiceCollection services, SqlServerServiceInfo info, IConfiguration config, ServiceLifetime contextLifetime)
        {
            Type SqlServerConnection = ConnectorHelpers.FindType(SqlServerAssemblies, SqlServerTypeNames);
            if (SqlServerConnection == null)
            {
                throw new ConnectorException("Unable to find SqlServerConnection, are you missing SqlServer ADO.NET assembly");
            }

            SqlServerProviderConnectorOptions SqlServerConfig = new SqlServerProviderConnectorOptions(config);
            SqlServerProviderConnectorFactory factory = new SqlServerProviderConnectorFactory(info, SqlServerConfig, SqlServerConnection);
            services.Add(new ServiceDescriptor(SqlServerConnection, factory.Create, contextLifetime));
        }
    }
}
