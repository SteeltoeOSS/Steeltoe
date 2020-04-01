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

namespace Steeltoe.Connector.MySql.EFCore
{
    public static class MySqlDbContextOptionsExtensions
    {
        public static DbContextOptionsBuilder UseMySql(this DbContextOptionsBuilder optionsBuilder, IConfiguration config, object mySqlOptionsAction = null)
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

            return DoUseMySql(optionsBuilder, connection, mySqlOptionsAction);
        }

        public static DbContextOptionsBuilder UseMySql(this DbContextOptionsBuilder optionsBuilder, IConfiguration config, string serviceName, object mySqlOptionsAction = null)
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

            return DoUseMySql(optionsBuilder, connection, mySqlOptionsAction);
        }

        public static DbContextOptionsBuilder<TContext> UseMySql<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, IConfiguration config, object mySqlOptionsAction = null)
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

            return DoUseMySql<TContext>(optionsBuilder, connection, mySqlOptionsAction);
        }

        public static DbContextOptionsBuilder<TContext> UseMySql<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, IConfiguration config, string serviceName, object mySqlOptionsAction = null)
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

            return DoUseMySql<TContext>(optionsBuilder, connection, mySqlOptionsAction);
        }

        private static MethodInfo FindUseSqlMethod(Type type, Type[] parameterTypes)
        {
            var typeInfo = type.GetTypeInfo();
            var declaredMethods = typeInfo.DeclaredMethods;

            foreach (var ci in declaredMethods)
            {
                var parameters = ci.GetParameters();
                if (parameters.Length == 3 &&
                    ci.Name.Equals("UseMySQL", StringComparison.InvariantCultureIgnoreCase) &&
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
                ? config.GetSingletonServiceInfo<MySqlServiceInfo>()
                : config.GetRequiredServiceInfo<MySqlServiceInfo>(serviceName);

            var mySqlConfig = new MySqlProviderConnectorOptions(config);

            var factory = new MySqlProviderConnectorFactory(info, mySqlConfig, null);

            return factory.CreateConnectionString();
        }

        private static DbContextOptionsBuilder DoUseMySql(DbContextOptionsBuilder builder, string connection, object mySqlOptionsAction = null)
        {
            var extensionType = EntityFrameworkCoreTypeLocator.MySqlDbContextOptionsType;

            var useMethod = FindUseSqlMethod(extensionType, new Type[] { typeof(DbContextOptionsBuilder), typeof(string) });
            if (extensionType == null)
            {
                throw new ConnectorException("Unable to find UseMySql extension, are you missing MySql EntityFramework Core assembly");
            }

            var result = ReflectionHelpers.Invoke(useMethod, null, new object[] { builder, connection, mySqlOptionsAction });
            if (result == null)
            {
                throw new ConnectorException(string.Format("Failed to invoke UseMySql extension, connection: {0}", connection));
            }

            return (DbContextOptionsBuilder)result;
        }

        private static DbContextOptionsBuilder<TContext> DoUseMySql<TContext>(DbContextOptionsBuilder<TContext> builder, string connection, object mySqlOptionsAction = null)
            where TContext : DbContext
        {
            return (DbContextOptionsBuilder<TContext>)DoUseMySql((DbContextOptionsBuilder)builder, connection, mySqlOptionsAction);
        }
    }
}
