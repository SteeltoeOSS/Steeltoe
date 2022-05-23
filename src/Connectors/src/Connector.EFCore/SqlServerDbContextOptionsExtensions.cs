// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

            var useMethod = FindUseSqlMethod(extensionType, new[] { typeof(DbContextOptionsBuilder), typeof(string) });
            if (extensionType == null)
            {
                throw new ConnectorException("Unable to find UseSqlServer extension, are you missing SqlServer EntityFramework Core assembly");
            }

            var result = ReflectionHelpers.Invoke(useMethod, null, new[] { builder, connection, sqlServerOptionsAction });
            if (result == null)
            {
                throw new ConnectorException($"Failed to invoke UseSqlServer extension, connection: {connection}");
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
