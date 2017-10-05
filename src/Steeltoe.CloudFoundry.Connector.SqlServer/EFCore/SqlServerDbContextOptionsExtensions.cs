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
using Microsoft.Extensions.Configuration;
using Steeltoe.CloudFoundry.Connector.Services;
using System;
using System.Reflection;

namespace Steeltoe.CloudFoundry.Connector.SqlServer.EFCore
{
    public static class SqlServerDbContextOptionsExtensions
    {
        public static DbContextOptionsBuilder UseSqlServer(this DbContextOptionsBuilder optionsBuilder, IConfiguration config, object SqlServerOptionsAction = null)
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

            return DoUseSqlServer(optionsBuilder, connection, SqlServerOptionsAction);
        }

        public static DbContextOptionsBuilder UseSqlServer(this DbContextOptionsBuilder optionsBuilder, IConfiguration config, string serviceName, object SqlServerOptionsAction = null)
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

            return DoUseSqlServer(optionsBuilder, connection, SqlServerOptionsAction);
        }

        public static DbContextOptionsBuilder<TContext> UseSqlServer<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, IConfiguration config, object SqlServerOptionsAction = null)
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

            return DoUseSqlServer<TContext>(optionsBuilder, connection, SqlServerOptionsAction);
        }

        public static DbContextOptionsBuilder<TContext> UseSqlServer<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, IConfiguration config, string serviceName, object SqlServerOptionsAction = null)
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

            return DoUseSqlServer<TContext>(optionsBuilder, connection, SqlServerOptionsAction);
        }

        private static string GetConnection(IConfiguration config, string serviceName = null)
        {
            SqlServerServiceInfo info = null;
            if (string.IsNullOrEmpty(serviceName))
            {
                info = config.GetSingletonServiceInfo<SqlServerServiceInfo>();
            }
            else
            {
                info = config.GetRequiredServiceInfo<SqlServerServiceInfo>(serviceName);
            }

            SqlServerProviderConnectorOptions SqlServerConfig = new SqlServerProviderConnectorOptions(config);

            SqlServerProviderConnectorFactory factory = new SqlServerProviderConnectorFactory(info, SqlServerConfig, null);

            return factory.CreateConnectionString();
        }

        private static string[] SqlServerEntityAssemblies = new string[] { "Microsoft.EntityFrameworkCore.SqlServer" };

        private static string[] SqlServerEntityTypeNames = new string[] { "Microsoft.EntityFrameworkCore.SqlServerDbContextOptionsExtensions" };

        private static DbContextOptionsBuilder DoUseSqlServer(DbContextOptionsBuilder builder, string connection, object SqlServerOptionsAction = null)
        {
            Type extensionType = ConnectorHelpers.FindType(SqlServerEntityAssemblies, SqlServerEntityTypeNames);
            if (extensionType == null)
            {
                throw new ConnectorException("Unable to find DbContextOptionsBuilder extension, are you missing SqlServer EntityFramework Core assembly");
            }

            MethodInfo useMethod = FindUseSqlMethod(extensionType, new Type[] { typeof(DbContextOptionsBuilder), typeof(string) });
            if (extensionType == null)
            {
                throw new ConnectorException("Unable to find UseSqlServer extension, are you missing SqlServer EntityFramework Core assembly");
            }

            object result = ConnectorHelpers.Invoke(useMethod, null, new object[] { builder, connection, SqlServerOptionsAction });
            if (result == null)
            {
                throw new ConnectorException(string.Format("Failed to invoke UseSqlServer extension, connection: {0}", connection));
            }

            return (DbContextOptionsBuilder)result;
        }

        private static DbContextOptionsBuilder<TContext> DoUseSqlServer<TContext>(DbContextOptionsBuilder<TContext> builder, string connection, object SqlServerOptionsAction = null)
            where TContext : DbContext
        {
            return (DbContextOptionsBuilder<TContext>)DoUseSqlServer((DbContextOptionsBuilder)builder, connection, SqlServerOptionsAction);
        }

        public static MethodInfo FindUseSqlMethod(Type type, Type[] parameterTypes)
        {
            var typeInfo = type.GetTypeInfo();
            var declaredMethods = typeInfo.DeclaredMethods;

            foreach (MethodInfo ci in declaredMethods)
            {
                var parameters = ci.GetParameters();
                if (parameters.Length == 3 &&
                        (ci.Name.Equals("UseSqlServer") || ci.Name.Equals("UseSqlServer")) &&
                        parameters[0].ParameterType.Equals(parameterTypes[0]) &&
                        parameters[1].ParameterType.Equals(parameterTypes[1]) &&
                        ci.IsPublic && ci.IsStatic)
                {
                    return ci;
                }
            }

            return null;
        }
    }
}
