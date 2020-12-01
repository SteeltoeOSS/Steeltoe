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

namespace Steeltoe.Connector.MySql.EFCore
{
    public static class MySqlDbContextOptionsExtensions
    {
        /// <summary>
        /// Configure Entity Framework Core to use a MySQL database
        /// </summary>
        /// <param name="optionsBuilder"><see cref="DbContextOptionsBuilder"/></param>
        /// <param name="config">Application configuration</param>
        /// <param name="mySqlOptionsAction">An action for customizing the MySqlDbContextOptionsBuilder</param>
        /// <returns><see cref="DbContextOptionsBuilder"/>, configured to use MySQL</returns>
        /// <remarks>
        ///   When used with EF Core 5.0, this method may result in the use of ServerVersion.AutoDetect(), which opens an extra connection to the server.<para />
        ///   Pass in a ServerVersion to avoid the extra DB Connection - see https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/issues/1088#issuecomment-726091533
        /// </remarks>
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

        /// <summary>
        /// Configure Entity Framework Core to use a MySQL database
        /// </summary>
        /// <param name="optionsBuilder"><see cref="DbContextOptionsBuilder"/></param>
        /// <param name="config">Application configuration</param>
        /// <param name="serverVersion">The version of MySQL/MariaDB to connect to (introduced in EF Core 5.0)</param>
        /// <param name="mySqlOptionsAction">An action for customizing the MySqlDbContextOptionsBuilder</param>
        /// <returns><see cref="DbContextOptionsBuilder"/>, configured to use MySQL</returns>
        public static DbContextOptionsBuilder UseMySql(this DbContextOptionsBuilder optionsBuilder, IConfiguration config, object serverVersion, object mySqlOptionsAction = null)
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

            return DoUseMySql(optionsBuilder, connection, mySqlOptionsAction, serverVersion);
        }

        /// <summary>
        /// Configure Entity Framework Core to use a MySQL database identified by a named service binding
        /// </summary>
        /// <param name="optionsBuilder"><see cref="DbContextOptionsBuilder"/></param>
        /// <param name="config">Application configuration</param>
        /// <param name="serviceName">The name of the service binding to use</param>
        /// <param name="mySqlOptionsAction">An action for customizing the MySqlDbContextOptionsBuilder</param>
        /// <returns><see cref="DbContextOptionsBuilder"/>, configured to use MySQL</returns>
        /// <remarks>
        ///   When used with EF Core 5.0, this method may result in the use of ServerVersion.AutoDetect(), which opens an extra connection to the server.<para />
        ///   Pass in a ServerVersion to avoid the extra DB Connection - see https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/issues/1088#issuecomment-726091533
        /// </remarks>
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

        /// <summary>
        /// Configure Entity Framework Core to use a MySQL database identified by a named service binding
        /// </summary>
        /// <param name="optionsBuilder"><see cref="DbContextOptionsBuilder"/></param>
        /// <param name="config">Application configuration</param>
        /// <param name="serviceName">The name of the service binding to use</param>
        /// <param name="serverVersion">The version of MySQL/MariaDB to connect to (introduced in EF Core 5.0)</param>
        /// <param name="mySqlOptionsAction">An action for customizing the MySqlDbContextOptionsBuilder</param>
        /// <returns><see cref="DbContextOptionsBuilder"/>, configured to use MySQL</returns>
        public static DbContextOptionsBuilder UseMySql(this DbContextOptionsBuilder optionsBuilder, IConfiguration config, string serviceName, object serverVersion, object mySqlOptionsAction = null)
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

            return DoUseMySql(optionsBuilder, connection, mySqlOptionsAction, serverVersion);
        }

        /// <summary>
        /// Configure Entity Framework Core to use a MySQL database
        /// </summary>
        /// <typeparam name="TContext">Type of <see cref="DbContext"/></typeparam>
        /// <param name="optionsBuilder"><see cref="DbContextOptionsBuilder"/></param>
        /// <param name="config">Application configuration</param>
        /// <param name="mySqlOptionsAction">An action for customizing the MySqlDbContextOptionsBuilder</param>
        /// <param name="serverVersion">The version of MySQL/MariaDB to connect to (introduced in EF Core 5.0)</param>
        /// <returns><see cref="DbContextOptionsBuilder"/>, configured to use MySQL</returns>
        public static DbContextOptionsBuilder<TContext> UseMySql<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, IConfiguration config, object mySqlOptionsAction = null, object serverVersion = null)
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

            return DoUseMySql(optionsBuilder, connection, mySqlOptionsAction, serverVersion);
        }

        /// <summary>
        /// Configure Entity Framework Core to use a MySQL database identified by a named service binding
        /// </summary>
        /// <typeparam name="TContext">Type of <see cref="DbContext"/></typeparam>
        /// <param name="optionsBuilder"><see cref="DbContextOptionsBuilder"/></param>
        /// <param name="config">Application configuration</param>
        /// <param name="serviceName">The name of the service binding to use</param>
        /// <param name="mySqlOptionsAction">An action for customizing the MySqlDbContextOptionsBuilder</param>
        /// <param name="serverVersion">The version of MySQL/MariaDB to connect to (introduced in EF Core 5.0)</param>
        /// <returns><see cref="DbContextOptionsBuilder"/>, configured to use MySQL</returns>
        public static DbContextOptionsBuilder<TContext> UseMySql<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, IConfiguration config, string serviceName, object mySqlOptionsAction = null, object serverVersion = null)
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

            return DoUseMySql(optionsBuilder, connection, mySqlOptionsAction, serverVersion);
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

        private static DbContextOptionsBuilder DoUseMySql(DbContextOptionsBuilder builder, string connection, object mySqlOptionsAction = null, object serverVersion = null)
        {
            var extensionType = EntityFrameworkCoreTypeLocator.MySqlDbContextOptionsType;

            MethodInfo useMethod;
            object[] parms;

            // the signature changed in 5.0 to require a param of type ServerVersion - use the presence of this new type to select the signature
            if (EntityFrameworkCoreTypeLocator.MySqlVersionType == null)
            {
                useMethod = FindUseSqlMethod(extensionType, new Type[] { typeof(DbContextOptionsBuilder), typeof(string) });
                parms = new object[] { builder, connection, mySqlOptionsAction };
            }
            else
            {
                // If the server version wasn't passed in, use the EF Core lib to autodetect it (this is the part that creates an extra connection)
                serverVersion ??= ReflectionHelpers.FindMethod(EntityFrameworkCoreTypeLocator.MySqlVersionType, "AutoDetect", new Type[] { typeof(string) }).Invoke(null, new[] { connection });
                useMethod = FindUseSqlMethod(extensionType, new Type[] { typeof(DbContextOptionsBuilder), typeof(string), EntityFrameworkCoreTypeLocator.MySqlVersionType, typeof(Action<DbContextOptionsBuilder>) });
                parms = new object[] { builder, connection, serverVersion, mySqlOptionsAction };
            }

            if (extensionType == null)
            {
                throw new ConnectorException("Unable to find UseMySql extension, are you missing MySql EntityFramework Core assembly");
            }

            var result = ReflectionHelpers.Invoke(useMethod, null, parms);
            if (result == null)
            {
                throw new ConnectorException(string.Format("Failed to invoke UseMySql extension, connection: {0}", connection));
            }

            return (DbContextOptionsBuilder)result;
        }

        private static DbContextOptionsBuilder<TContext> DoUseMySql<TContext>(DbContextOptionsBuilder<TContext> builder, string connection, object mySqlOptionsAction = null, object serverVersion = null)
            where TContext : DbContext
                => (DbContextOptionsBuilder<TContext>)DoUseMySql((DbContextOptionsBuilder)builder, connection, mySqlOptionsAction, serverVersion);

        private static MethodInfo FindUseSqlMethod(Type type, Type[] parameterTypes)
        {
            var typeInfo = type.GetTypeInfo();
            var declaredMethods = typeInfo.DeclaredMethods;

            foreach (var ci in declaredMethods)
            {
                var parameters = ci.GetParameters();
                if ((parameters.Length == 3 || parameters.Length == parameterTypes.Length) &&
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
    }
}
