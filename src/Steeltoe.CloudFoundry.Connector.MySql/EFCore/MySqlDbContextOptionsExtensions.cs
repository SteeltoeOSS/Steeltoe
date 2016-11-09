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

#if !NET451
using System;

using MySQL.Data.EntityFrameworkCore.Infraestructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Steeltoe.CloudFoundry.Connector.Services;
using MySQL.Data.EntityFrameworkCore.Extensions;

namespace Steeltoe.CloudFoundry.Connector.MySql.EFCore
{
    public static class MySqlDbContextOptionsExtensions
    {
        public static DbContextOptionsBuilder UseMySql(this DbContextOptionsBuilder optionsBuilder, IConfiguration config, Action<MySQLDbContextOptionsBuilder> mySqlOptionsAction = null)
        {
            if (optionsBuilder == null)
            {
                throw new ArgumentNullException(nameof(optionsBuilder));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var connection = GetConnection(config);

            return optionsBuilder.UseMySQL(connection, mySqlOptionsAction);

        }
        public static DbContextOptionsBuilder UseMySql(this DbContextOptionsBuilder optionsBuilder, IConfiguration config, string serviceName, Action<MySQLDbContextOptionsBuilder> mySqlOptionsAction = null)
        {
            if (optionsBuilder == null)
            {
                throw new ArgumentNullException(nameof(optionsBuilder));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentException(nameof(serviceName));
            }

            var connection = GetConnection(config, serviceName);

            return optionsBuilder.UseMySQL(connection, mySqlOptionsAction);

        }

        public static DbContextOptionsBuilder<TContext> UseMySql<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, IConfiguration config, Action<MySQLDbContextOptionsBuilder> mySqlOptionsAction = null) where TContext : DbContext
        {
            if (optionsBuilder == null)
            {
                throw new ArgumentNullException(nameof(optionsBuilder));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var connection = GetConnection(config);

            return optionsBuilder.UseMySQL<TContext>(connection, mySqlOptionsAction);

        }
        public static DbContextOptionsBuilder<TContext> UseMySql<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, IConfiguration config, string serviceName, Action<MySQLDbContextOptionsBuilder> mySqlOptionsAction = null) where TContext : DbContext
        {
            if (optionsBuilder == null)
            {
                throw new ArgumentNullException(nameof(optionsBuilder));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentException(nameof(serviceName));
            }

            var connection = GetConnection(config, serviceName);

            return optionsBuilder.UseMySQL<TContext>(connection, mySqlOptionsAction);

        }

        private static string GetConnection(IConfiguration config, string serviceName = null)
        {
            MySqlServiceInfo info = null;
            if (string.IsNullOrEmpty(serviceName))
                info = config.GetSingletonServiceInfo<MySqlServiceInfo>();
            else
                info = config.GetRequiredServiceInfo<MySqlServiceInfo>(serviceName);

            MySqlProviderConnectorOptions mySqlConfig = new MySqlProviderConnectorOptions(config);

            MySqlProviderConnectorFactory factory = new MySqlProviderConnectorFactory(info, mySqlConfig);
            return factory.CreateConnectionString();
        }

    }
}
#endif