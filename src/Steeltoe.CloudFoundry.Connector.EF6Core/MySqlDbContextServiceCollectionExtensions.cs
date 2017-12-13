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

namespace Steeltoe.CloudFoundry.Connector.MySql.EF6
{
    public static class MySqlDbContextServiceCollectionExtensions
    {
        public static IServiceCollection AddDbContext<TContext>(this IServiceCollection services, IConfiguration config, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ILoggerFactory logFactory = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            MySqlServiceInfo info = config.GetSingletonServiceInfo<MySqlServiceInfo>();
            DoAdd(services, config, info, typeof(TContext), contextLifetime);

            return services;
        }

        public static IServiceCollection AddDbContext<TContext>(this IServiceCollection services, IConfiguration config, string serviceName, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ILoggerFactory logFactory = null)
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

            MySqlServiceInfo info = config.GetRequiredServiceInfo<MySqlServiceInfo>(serviceName);
            DoAdd(services, config, info, typeof(TContext), contextLifetime);

            return services;
        }

        private static void DoAdd(IServiceCollection services, IConfiguration config, MySqlServiceInfo info, Type dbContextType, ServiceLifetime contextLifetime)
        {
            MySqlProviderConnectorOptions mySqlConfig = new MySqlProviderConnectorOptions(config);

            MySqlDbContextConnectorFactory factory = new MySqlDbContextConnectorFactory(info, mySqlConfig, dbContextType);
            services.Add(new ServiceDescriptor(dbContextType, factory.Create, contextLifetime));
        }
    }
}
