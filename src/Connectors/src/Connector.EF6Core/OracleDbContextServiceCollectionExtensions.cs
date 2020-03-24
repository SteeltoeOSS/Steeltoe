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
using Steeltoe.Connector.Oracle;
using Steeltoe.Connector.Oracle.EF6;
using Steeltoe.Connector.Services;
using System;

namespace Steeltoe.Connector.EF6Core
{
    public static class OracleDbContextServiceCollectionExtensions
    {
        /// <summary>
        /// Add a Oracle-backed DbContext and Oracle health contributor to the Service Collection
        /// </summary>
        /// <typeparam name="TContext">Type of DbContext to add</typeparam>
        /// <param name="services">Service Collection</param>
        /// <param name="config">Application Configuration</param>
        /// <param name="contextLifetime">Lifetime of the service to inject</param>
        /// <returns>IServiceCollection for chaining</returns>
        public static IServiceCollection AddDbContext<TContext>(this IServiceCollection services, IConfiguration config, ServiceLifetime contextLifetime = ServiceLifetime.Scoped)
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
            DoAdd(services, config, info, typeof(TContext), contextLifetime);

            return services;
        }

        /// <summary>
        /// Add a Oracle-backed DbContext and Oracle health contributor to the Service Collection
        /// </summary>
        /// <typeparam name="TContext">Type of DbContext to add</typeparam>
        /// <param name="services">Service Collection</param>
        /// <param name="config">Application Configuration</param>
        /// <param name="serviceName">Name of service binding in Cloud Foundry</param>
        /// <param name="contextLifetime">Lifetime of the service to inject</param>
        /// <returns>IServiceCollection for chaining</returns>
        public static IServiceCollection AddDbContext<TContext>(this IServiceCollection services, IConfiguration config, string serviceName, ServiceLifetime contextLifetime = ServiceLifetime.Scoped)
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
            DoAdd(services, config, info, typeof(TContext), contextLifetime);

            return services;
        }

        private static void DoAdd(IServiceCollection services, IConfiguration config, OracleServiceInfo info, Type dbContextType, ServiceLifetime contextLifetime)
        {
            var oracleConfig = new OracleProviderConnectorOptions(config);

            var factory = new OracleDbContextConnectorFactory(info, oracleConfig, dbContextType);
            services.Add(new ServiceDescriptor(dbContextType, factory.Create, contextLifetime));
        }
    }
}