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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector.Relational;
using Steeltoe.CloudFoundry.Connector.Relational.PostgreSql;
using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.Common.HealthChecks;
using System;
using System.Data;

namespace Steeltoe.CloudFoundry.Connector.PostgreSql
{
    public static class PostgresProviderServiceCollectionExtensions
    {
        /// <summary>
        /// Add NpgsqlConnection and its IHealthContributor to a ServiceCollection
        /// </summary>
        /// <param name="services">Service collection to add to</param>
        /// <param name="config">App configuration</param>
        /// <param name="contextLifetime">Lifetime of the service to inject</param>
        /// <param name="logFactory">logger factory</param>
        /// <param name="healthChecksBuilder">Microsoft HealthChecksBuilder</param>
        /// <returns>IServiceCollection for chaining</returns>
        /// <remarks>NpgsqlConnection is retrievable as both NpgsqlConnection and IDbConnection</remarks>
        public static IServiceCollection AddPostgresConnection(this IServiceCollection services, IConfiguration config, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ILoggerFactory logFactory = null, IHealthChecksBuilder healthChecksBuilder = null)
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

            DoAdd(services, info, config, contextLifetime, healthChecksBuilder);
            return services;
        }

        /// <summary>
        /// Add NpgsqlConnection and its IHealthContributor to a ServiceCollection
        /// </summary>
        /// <param name="services">Service collection to add to</param>
        /// <param name="config">App configuration</param>
        /// <param name="serviceName">cloud foundry service name binding</param>
        /// <param name="contextLifetime">Lifetime of the service to inject</param>
        /// <param name="logFactory">logger factory</param>
        /// <param name="healthChecksBuilder">Microsoft HealthChecksBuilder</param>
        /// <returns>IServiceCollection for chaining</returns>
        /// <remarks>NpgsqlConnection is retrievable as both NpgsqlConnection and IDbConnection</remarks>
        public static IServiceCollection AddPostgresConnection(this IServiceCollection services, IConfiguration config, string serviceName, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ILoggerFactory logFactory = null, IHealthChecksBuilder healthChecksBuilder = null)
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

            DoAdd(services, info, config, contextLifetime, healthChecksBuilder);
            return services;
        }

        private static void DoAdd(IServiceCollection services, PostgresServiceInfo info, IConfiguration config, ServiceLifetime contextLifetime, IHealthChecksBuilder healthChecksBuilder)
        {
            Type postgresConnection = ConnectorHelpers.FindType(PostgreSqlTypeLocator.Assemblies, PostgreSqlTypeLocator.ConnectionTypeNames);
            var postgresConfig = new PostgresProviderConnectorOptions(config);
            var factory = new PostgresProviderConnectorFactory(info, postgresConfig, postgresConnection);
            services.Add(new ServiceDescriptor(typeof(IDbConnection), factory.Create, contextLifetime));
            services.Add(new ServiceDescriptor(postgresConnection, factory.Create, contextLifetime));
            if (healthChecksBuilder == null)
            {
                services.Add(new ServiceDescriptor(typeof(IHealthContributor), ctx => new RelationalHealthContributor((IDbConnection)factory.Create(ctx), ctx.GetService<ILogger<RelationalHealthContributor>>()), ServiceLifetime.Singleton));
            }
            else
            {
                healthChecksBuilder.AddNpgSql(factory.CreateConnectionString());
            }
        }
    }
}
