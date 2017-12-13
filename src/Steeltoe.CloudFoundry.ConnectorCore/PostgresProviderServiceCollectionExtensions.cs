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

namespace Steeltoe.CloudFoundry.Connector.PostgreSql
{
    public static class PostgresProviderServiceCollectionExtensions
    {
        private static string[] postgresAssemblies = new string[] { "Npgsql" };
        private static string[] postgresTypeNames = new string[] { "Npgsql.NpgsqlConnection" };

        /// <summary>
        /// Add Postgres to a ServiceCollection
        /// </summary>
        /// <param name="services">Service collection to add to</param>
        /// <param name="config">App configuration</param>
        /// <param name="registerInterface">Optionally disable registering the interface type with DI</param>
        /// <param name="contextLifetime">Lifetime of the service to inject</param>
        /// <param name="logFactory">logger factory</param>
        /// <returns>IServiceCollection for chaining</returns>
        public static IServiceCollection AddPostgresConnection(this IServiceCollection services, IConfiguration config, bool registerInterface = true, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ILoggerFactory logFactory = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            PostgresServiceInfo info = config.GetSingletonServiceInfo<PostgresServiceInfo>();

            DoAdd(services, info, config, registerInterface, contextLifetime);
            return services;
        }

        /// <summary>
        /// Add Postgres to a ServiceCollection
        /// </summary>
        /// <param name="services">Service collection to add to</param>
        /// <param name="config">App configuration</param>
        /// <param name="serviceName">cloud foundry service name binding</param>
        /// <param name="registerInterface">Optionally disable registering the interface type with DI</param>
        /// <param name="contextLifetime">Lifetime of the service to inject</param>
        /// <param name="logFactory">logger factory</param>
        /// <returns>IServiceCollection for chaining</returns>
        public static IServiceCollection AddPostgresConnection(this IServiceCollection services, IConfiguration config, string serviceName, bool registerInterface = true, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ILoggerFactory logFactory = null)
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

            PostgresServiceInfo info = config.GetRequiredServiceInfo<PostgresServiceInfo>(serviceName);

            DoAdd(services, info, config, registerInterface, contextLifetime);
            return services;
        }

        private static void DoAdd(IServiceCollection services, PostgresServiceInfo info, IConfiguration config, bool registerInterface, ServiceLifetime contextLifetime)
        {
            Type postgresConnection = ConnectorHelpers.FindType(postgresAssemblies, postgresTypeNames);
            if (postgresConnection == null)
            {
                throw new ConnectorException("Unable to find NpgsqlConnection, are you missing Postgres ADO.NET assembly");
            }

            PostgresProviderConnectorOptions postgresConfig = new PostgresProviderConnectorOptions(config);
            PostgresProviderConnectorFactory factory = new PostgresProviderConnectorFactory(info, postgresConfig, postgresConnection);
            if (registerInterface)
            {
                services.Add(new ServiceDescriptor(typeof(IDbConnection), factory.Create, contextLifetime));
            }

            services.Add(new ServiceDescriptor(postgresConnection, factory.Create, contextLifetime));
        }
    }
}
