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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Npgsql;
using SteelToe.CloudFoundry.Connector.Services;
using System;


namespace SteelToe.CloudFoundry.Connector.PostgreSql.EFCore
{
    public static class PostgresDbContextOptionsExtensions
    {
        public static DbContextOptionsBuilder UseNpgsql(this DbContextOptionsBuilder optionsBuilder, IConfiguration config, Action<NpgsqlDbContextOptionsBuilder> npgsqlOptionsAction = null)
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

            return optionsBuilder.UseNpgsql(connection, npgsqlOptionsAction);

        }
        public static DbContextOptionsBuilder UseNpgsql(this DbContextOptionsBuilder optionsBuilder, IConfiguration config, string serviceName, Action<NpgsqlDbContextOptionsBuilder> npgsqlOptionsAction = null)
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

            return optionsBuilder.UseNpgsql(connection, npgsqlOptionsAction);

        }

        public static DbContextOptionsBuilder<TContext> UseNpgsql<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, IConfiguration config, Action<NpgsqlDbContextOptionsBuilder> npgsqlOptionsAction = null) where TContext : DbContext
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

            return optionsBuilder.UseNpgsql<TContext>(connection, npgsqlOptionsAction);

        }
        public static DbContextOptionsBuilder<TContext> UseNpgsql<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, IConfiguration config, string serviceName, Action<NpgsqlDbContextOptionsBuilder> npgsqlOptionsAction = null) where TContext : DbContext
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

            return optionsBuilder.UseNpgsql<TContext>(connection, npgsqlOptionsAction);

        }

        private static NpgsqlConnection GetConnection(IConfiguration config, string serviceName = null)
        {
            PostgresServiceInfo info = null;
            if (string.IsNullOrEmpty(serviceName))
                info = config.GetSingletonServiceInfo<PostgresServiceInfo>();
            else
                info = config.GetRequiredServiceInfo<PostgresServiceInfo>(serviceName);

            PostgresProviderConnectorOptions postgresConfig = new PostgresProviderConnectorOptions(config);

            PostgresProviderConnectorFactory factory = new PostgresProviderConnectorFactory(info, postgresConfig);
            return factory.Create(null) as NpgsqlConnection;
        }

    }
}
