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
using System.Data;

namespace Steeltoe.CloudFoundry.Connector.SqlServer
{
    public static class SqlServerProviderServiceCollectionExtensions
    {
        private static string[] sqlServerAssemblies = new string[] { "System.Data.SqlClient" };

        private static string[] sqlServerTypeNames = new string[] { "System.Data.SqlClient.SqlConnection" };

        /// <summary>
        /// Add SQL Server to a ServiceCollection
        /// </summary>
        /// <param name="services">Service collection to add to</param>
        /// <param name="config">App configuration</param>
        /// <param name="registerInterface">Optionally disable registering the interface type with DI</param>
        /// <param name="contextLifetime">Lifetime of the service to inject</param>
        /// <param name="logFactory">logger factory</param>
        /// <returns>IServiceCollection for chaining</returns>
        public static IServiceCollection AddSqlServerConnection(this IServiceCollection services, IConfiguration config, bool registerInterface = true, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ILoggerFactory logFactory = null)
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
            DoAdd(services, info, config, registerInterface, contextLifetime);

            return services;
        }

        /// <summary>
        /// Add SQL Server to a ServiceCollection
        /// </summary>
        /// <param name="services">Service collection to add to</param>
        /// <param name="config">App configuration</param>
        /// <param name="serviceName">cloud foundry service name binding</param>
        /// <param name="registerInterface">Optionally disable registering the interface type with DI</param>
        /// <param name="contextLifetime">Lifetime of the service to inject</param>
        /// <param name="logFactory">logger factory</param>
        /// <returns>IServiceCollection for chaining</returns>
        public static IServiceCollection AddSqlServerConnection(this IServiceCollection services, IConfiguration config, string serviceName, bool registerInterface = true, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ILoggerFactory logFactory = null)
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
            DoAdd(services, info, config, registerInterface, contextLifetime);

            return services;
        }

        private static void DoAdd(IServiceCollection services, SqlServerServiceInfo info, IConfiguration config, bool registerInterface, ServiceLifetime contextLifetime)
        {
            Type sqlServerConnection = ConnectorHelpers.FindType(sqlServerAssemblies, sqlServerTypeNames);
            if (sqlServerConnection == null)
            {
                throw new ConnectorException("Unable to find SqlServerConnection, are you missing SqlServer ADO.NET assembly");
            }

            SqlServerProviderConnectorOptions sqlServerConfig = new SqlServerProviderConnectorOptions(config);
            SqlServerProviderConnectorFactory factory = new SqlServerProviderConnectorFactory(info, sqlServerConfig, sqlServerConnection);
            if (registerInterface)
            {
                services.Add(new ServiceDescriptor(typeof(IDbConnection), factory.Create, contextLifetime));
            }

            services.Add(new ServiceDescriptor(sqlServerConnection, factory.Create, contextLifetime));
        }
    }
}
