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
using Steeltoe.CloudFoundry.Connector.EFCore;
using Steeltoe.CloudFoundry.Connector.Services;
using System;
using System.Reflection;

namespace Steeltoe.CloudFoundry.Connector.Oracle.EFCore
{
    public static class OracleDbContextOptionsExtensions
    {
        public static DbContextOptionsBuilder UseOracle(this DbContextOptionsBuilder optionsBuilder, IConfiguration config, object oracleOptionsAction = null)
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

            return DoUseOracle(optionsBuilder, connection, oracleOptionsAction);
        }

        public static DbContextOptionsBuilder UseOracle(this DbContextOptionsBuilder optionsBuilder, IConfiguration config, string serviceName, object oracleOptionsAction = null)
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

            return DoUseOracle(optionsBuilder, connection, oracleOptionsAction);
        }

        public static DbContextOptionsBuilder<TContext> UseOracle<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, IConfiguration config, object oracleOptionsAction = null)
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

            return DoUseOracle<TContext>(optionsBuilder, connection, oracleOptionsAction);
        }

        public static DbContextOptionsBuilder<TContext> UseOracle<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, IConfiguration config, string serviceName, object oracleOptionsAction = null)
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

            return DoUseOracle<TContext>(optionsBuilder, connection, oracleOptionsAction);
        }

        private static DbContextOptionsBuilder DoUseOracle(DbContextOptionsBuilder optionsBuilder, object connection, object oracleOptionsAction)
        {
            Type extensionType = EntityFrameworkCoreTypeLocator.OracleDbContextOptionsType;

            MethodInfo useMethod = FindUseSqlMethod(extensionType, new Type[] { typeof(DbContextOptionsBuilder), typeof(string) });
            if (extensionType == null)
            {
                throw new ConnectorException("Unable to find UseOracle extension, are you missing Oracle EntityFramework Core assembly");
            }

            object result = ConnectorHelpers.Invoke(useMethod, null, new object[] { optionsBuilder, connection, oracleOptionsAction });
            if (result == null)
            {
                throw new ConnectorException(string.Format("Failed to invoke UseOracle extension, connection: {0}", connection));
            }

            return (DbContextOptionsBuilder)result;
        }

        private static DbContextOptionsBuilder<TContext> DoUseOracle<TContext>(DbContextOptionsBuilder<TContext> optionsBuilder, string connection, object oracleOptionsAction = null)
            where TContext : DbContext
        {
            return (DbContextOptionsBuilder<TContext>)DoUseOracle((DbContextOptionsBuilder)optionsBuilder, connection, oracleOptionsAction);
        }

        private static MethodInfo FindUseSqlMethod(Type type, Type[] parameterTypes)
        {
            var typeInfo = type.GetTypeInfo();
            var declaredMethods = typeInfo.DeclaredMethods;

            foreach (MethodInfo ci in declaredMethods)
            {
                var parameters = ci.GetParameters();
                if (parameters.Length == 3 &&
                    ci.Name.Equals("UseOracle", StringComparison.InvariantCultureIgnoreCase) &&
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
            OracleServiceInfo info = string.IsNullOrEmpty(serviceName)
                ? config.GetSingletonServiceInfo<OracleServiceInfo>()
                : config.GetRequiredServiceInfo<OracleServiceInfo>(serviceName);

            OracleProviderConnectorOptions oracleConfig = new OracleProviderConnectorOptions(config);

            OracleProviderConnectorFactory factory = new OracleProviderConnectorFactory(info, oracleConfig, null);

            return factory.CreateConnectionString();
        }
    }
}
