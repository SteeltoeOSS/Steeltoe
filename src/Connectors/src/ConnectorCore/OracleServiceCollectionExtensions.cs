﻿// Copyright 2017 the original author or authors.
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
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connector.Services;
using System;
using System.Data;

namespace Steeltoe.CloudFoundry.Connector.Oracle
{
    public static class OracleServiceCollectionExtensions
    {
        /// <summary>
        /// Add an IHealthContributor to a ServiceCollection for Oracle
        /// </summary>
        /// <param name="services">Service collection to add to</param>
        /// <param name="config">App configuration</param>
        /// <param name="contextLifetime">Lifetime of the service to inject</param>
        /// <returns>IServiceCollection for chaining</returns>
        public static IServiceCollection AddOracleHealthContributor(this IServiceCollection services, IConfiguration config, ServiceLifetime contextLifetime = ServiceLifetime.Singleton)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var info = config.GetSingletonServiceInfo<OracleServiceInfo>();

            DoAdd(services, info, config, contextLifetime);
            return services;
        }

        /// <summary>
        /// Add an IHealthContributor to a ServiceCollection for Oracle
        /// </summary>
        /// <param name="services">Service collection to add to</param>
        /// <param name="config">App configuration</param>
        /// <param name="serviceName">cloud foundry service name binding</param>
        /// <param name="contextLifetime">Lifetime of the service to inject</param>
        /// <returns>IServiceCollection for chaining</returns>
        public static IServiceCollection AddOracleHealthContributor(this IServiceCollection services, IConfiguration config, string serviceName, ServiceLifetime contextLifetime = ServiceLifetime.Singleton)
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

            var info = config.GetRequiredServiceInfo<OracleServiceInfo>(serviceName);

            DoAdd(services, info, config, contextLifetime);
            return services;
        }

        private static void DoAdd(IServiceCollection services, OracleServiceInfo info, IConfiguration config, ServiceLifetime contextLifetime)
        {
            var oracleConfig = new OracleProviderConnectorOptions(config);
            var factory = new OracleProviderConnectorFactory(info, oracleConfig, OracleTypeLocator.OracleConnection);
            services.Add(new ServiceDescriptor(typeof(IHealthContributor), ctx => new RelationalHealthContributor((IDbConnection)factory.Create(ctx), ctx.GetService<ILogger<RelationalHealthContributor>>()), contextLifetime));
        }
    }
}
