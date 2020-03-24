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

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Reflection;
using Steeltoe.Connector.EFCore;
using Steeltoe.Connector.Services;
using System;
using System.Reflection;

namespace Steeltoe.Connector.SqlServer.EFCore
{
    public static class SqlServerDbContextOptionsExtensions
    {
        public static DbContextOptionsBuilder UseSqlServer(this DbContextOptionsBuilder optionsBuilder, IConfiguration config, object sqlServerOptionsAction = null)
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

            return DoUseSqlServer(optionsBuilder, connection, sqlServerOptionsAction);
        }

        public static DbContextOptionsBuilder UseSqlServer(this DbContextOptionsBuilder optionsBuilder, IConfiguration config, string serviceName, object sqlServerOptionsAction = null)
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

            return DoUseSqlServer(optionsBuilder, connection, sqlServerOptionsAction);
        }

        public static DbContextOptionsBuilder<TContext> UseSqlServer<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, IConfiguration config, object sqlServerOptionsAction = null)
            where TContext : DbContext
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

            return DoUseSqlServer<TContext>(optionsBuilder, connection, sqlServerOptionsAction);
        }

        public static DbContextOptionsBuilder<TContext> UseSqlServer<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, IConfiguration config, string serviceName, object sqlServerOptionsAction = null)
            where TContext : DbContext
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

            return DoUseSqlServer<TContext>(optionsBuilder, connection, sqlServerOptionsAction);
        }

        private static MethodInfo FindUseSqlMethod(Type type, Type[] parameterTypes)
        {
            var typeInfo = type.GetTypeInfo();
            var declaredMethods = typeInfo.DeclaredMethods;

            foreach (var ci in declaredMethods)
            {
                var parameters = ci.GetParameters();
                if (parameters.Length == 3 &&
                    ci.Name.Equals("UseSqlServer", StringComparison.InvariantCultureIgnoreCase) &&
                    parameters[0].ParameterType.Equals(parameterTypes[0]) &&
                    parameters[1].ParameterType.Equals(parameterTypes[1]) &&
                    ci.IsPublic && ci.IsStatic)
                {
                    return ci;
                }
            }

            return null;
        }

        private static string GetConnection(IConfiguration config, string serviceName = null)
        {
            var info = string.IsNullOrEmpty(serviceName)
                ? config.GetSingletonServiceInfo<SqlServerServiceInfo>()
                : config.GetRequiredServiceInfo<SqlServerServiceInfo>(serviceName);

            var sqlServerConfig = new SqlServerProviderConnectorOptions(config);

            var factory = new SqlServerProviderConnectorFactory(info, sqlServerConfig, null);

            return factory.CreateConnectionString();
        }

        private static DbContextOptionsBuilder DoUseSqlServer(DbContextOptionsBuilder builder, string connection, object sqlServerOptionsAction = null)
        {
            var extensionType = EntityFrameworkCoreTypeLocator.SqlServerDbContextOptionsType;

            var useMethod = FindUseSqlMethod(extensionType, new Type[] { typeof(DbContextOptionsBuilder), typeof(string) });
            if (extensionType == null)
            {
                throw new ConnectorException("Unable to find UseSqlServer extension, are you missing SqlServer EntityFramework Core assembly");
            }

            var result = ReflectionHelpers.Invoke(useMethod, null, new object[] { builder, connection, sqlServerOptionsAction });
            if (result == null)
            {
                throw new ConnectorException(string.Format("Failed to invoke UseSqlServer extension, connection: {0}", connection));
            }

            return (DbContextOptionsBuilder)result;
        }

        private static DbContextOptionsBuilder<TContext> DoUseSqlServer<TContext>(DbContextOptionsBuilder<TContext> builder, string connection, object sqlServerOptionsAction = null)
            where TContext : DbContext
        {
            return (DbContextOptionsBuilder<TContext>)DoUseSqlServer((DbContextOptionsBuilder)builder, connection, sqlServerOptionsAction);
        }
    }
}
