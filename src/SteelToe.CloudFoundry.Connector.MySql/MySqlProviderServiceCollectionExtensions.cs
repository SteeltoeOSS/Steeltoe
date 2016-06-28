//
// Copyright 2015 the original author or authors.
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
//

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using SteelToe.CloudFoundry.Connector.Services;
using System;


namespace SteelToe.CloudFoundry.Connector.MySql
{
    public static class MySqlProviderServiceCollectionExtensions
    {

        public static IServiceCollection AddMySqlConnection(this IServiceCollection services, IConfiguration config, ILoggerFactory logFactory = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }


            MySqlProviderConfiguration mySqlConfig = new MySqlProviderConfiguration(config);
            MySqlServiceInfo info = config.GetSingletonServiceInfo<MySqlServiceInfo>();
            MySqlProviderConnectorFactory factory = new MySqlProviderConnectorFactory(info, mySqlConfig);
            services.AddScoped(typeof(MySqlConnection), factory.Create);
            return services;
        }

        public static IServiceCollection AddMySqlConnection(this IServiceCollection services, IConfiguration config, string serviceName, ILoggerFactory logFactory = null)
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

            MySqlProviderConfiguration mySqlConfig = new MySqlProviderConfiguration(config);
            MySqlServiceInfo info = config.GetRequiredServiceInfo<MySqlServiceInfo>(serviceName);
            MySqlProviderConnectorFactory factory = new MySqlProviderConnectorFactory(info, mySqlConfig);
            services.AddScoped(typeof(MySqlConnection), factory.Create);
            return services;
        }

    }
}
