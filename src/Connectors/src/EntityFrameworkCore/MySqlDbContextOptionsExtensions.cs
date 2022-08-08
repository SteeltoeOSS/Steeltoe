// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Common.Reflection;
using Steeltoe.Connector.EntityFrameworkCore;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.MySql.EntityFrameworkCore;

public static class MySqlDbContextOptionsExtensions
{
    /// <summary>
    /// Configure Entity Framework Core to use a MySQL database.
    /// </summary>
    /// <param name="optionsBuilder">
    /// <see cref="DbContextOptionsBuilder" />.
    /// </param>
    /// <param name="config">
    /// Application configuration.
    /// </param>
    /// <param name="mySqlOptionsAction">
    /// An action for customizing the MySqlDbContextOptionsBuilder.
    /// </param>
    /// <returns>
    /// <see cref="DbContextOptionsBuilder" />, configured to use MySQL.
    /// </returns>
    /// <remarks>
    /// When used with EF Core 5.0, this method may result in the use of ServerVersion.AutoDetect(), which opens an extra connection to the server.
    /// <para />
    /// Pass in a ServerVersion to avoid the extra DB Connection - see
    /// https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/issues/1088#issuecomment-726091533.
    /// </remarks>
    public static DbContextOptionsBuilder UseMySql(this DbContextOptionsBuilder optionsBuilder, IConfiguration config, object mySqlOptionsAction = null)
    {
        return UseMySql(optionsBuilder, config, serverVersion: null, mySqlOptionsAction);
    }

    /// <summary>
    /// Configure Entity Framework Core to use a MySQL database.
    /// </summary>
    /// <param name="optionsBuilder">
    /// <see cref="DbContextOptionsBuilder" />.
    /// </param>
    /// <param name="config">
    /// Application configuration.
    /// </param>
    /// <param name="serverVersion">
    /// The version of MySQL/MariaDB to connect to (introduced in EF Core 5.0).
    /// </param>
    /// <param name="mySqlOptionsAction">
    /// An action for customizing the MySqlDbContextOptionsBuilder.
    /// </param>
    /// <returns>
    /// <see cref="DbContextOptionsBuilder" />, configured to use MySQL.
    /// </returns>
    public static DbContextOptionsBuilder UseMySql(this DbContextOptionsBuilder optionsBuilder, IConfiguration config, object serverVersion,
        object mySqlOptionsAction = null)
    {
        ArgumentGuard.NotNull(optionsBuilder);
        ArgumentGuard.NotNull(config);

        string connection = GetConnection(config);

        return DoUseMySql(optionsBuilder, connection, mySqlOptionsAction, serverVersion);
    }

    /// <summary>
    /// Configure Entity Framework Core to use a MySQL database identified by a named service binding.
    /// </summary>
    /// <param name="optionsBuilder">
    /// <see cref="DbContextOptionsBuilder" />.
    /// </param>
    /// <param name="config">
    /// Application configuration.
    /// </param>
    /// <param name="serviceName">
    /// The name of the service binding to use.
    /// </param>
    /// <param name="mySqlOptionsAction">
    /// An action for customizing the MySqlDbContextOptionsBuilder.
    /// </param>
    /// <returns>
    /// <see cref="DbContextOptionsBuilder" />, configured to use MySQL.
    /// </returns>
    /// <remarks>
    /// When used with EF Core 5.0, this method may result in the use of ServerVersion.AutoDetect(), which opens an extra connection to the server.
    /// <para />
    /// Pass in a ServerVersion to avoid the extra DB Connection - see
    /// https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/issues/1088#issuecomment-726091533.
    /// </remarks>
    public static DbContextOptionsBuilder UseMySql(this DbContextOptionsBuilder optionsBuilder, IConfiguration config, string serviceName,
        object mySqlOptionsAction = null)
    {
        return UseMySql(optionsBuilder, config, serviceName, null, mySqlOptionsAction);
    }

    /// <summary>
    /// Configure Entity Framework Core to use a MySQL database identified by a named service binding.
    /// </summary>
    /// <param name="optionsBuilder">
    /// <see cref="DbContextOptionsBuilder" />.
    /// </param>
    /// <param name="config">
    /// Application configuration.
    /// </param>
    /// <param name="serviceName">
    /// The name of the service binding to use.
    /// </param>
    /// <param name="serverVersion">
    /// The version of MySQL/MariaDB to connect to (introduced in EF Core 5.0).
    /// </param>
    /// <param name="mySqlOptionsAction">
    /// An action for customizing the MySqlDbContextOptionsBuilder.
    /// </param>
    /// <returns>
    /// <see cref="DbContextOptionsBuilder" />, configured to use MySQL.
    /// </returns>
    public static DbContextOptionsBuilder UseMySql(this DbContextOptionsBuilder optionsBuilder, IConfiguration config, string serviceName, object serverVersion,
        object mySqlOptionsAction = null)
    {
        ArgumentGuard.NotNull(optionsBuilder);
        ArgumentGuard.NotNull(config);

        if (string.IsNullOrEmpty(serviceName))
        {
            throw new ArgumentException(nameof(serviceName));
        }

        string connection = GetConnection(config, serviceName);

        return DoUseMySql(optionsBuilder, connection, mySqlOptionsAction, serverVersion);
    }

    /// <summary>
    /// Configure Entity Framework Core to use a MySQL database.
    /// </summary>
    /// <typeparam name="TContext">
    /// Type of <see cref="DbContext" />.
    /// </typeparam>
    /// <param name="optionsBuilder">
    /// <see cref="DbContextOptionsBuilder" />.
    /// </param>
    /// <param name="config">
    /// Application configuration.
    /// </param>
    /// <param name="mySqlOptionsAction">
    /// An action for customizing the MySqlDbContextOptionsBuilder.
    /// </param>
    /// <param name="serverVersion">
    /// The version of MySQL/MariaDB to connect to (introduced in EF Core 5.0).
    /// </param>
    /// <returns>
    /// <see cref="DbContextOptionsBuilder" />, configured to use MySQL.
    /// </returns>
    public static DbContextOptionsBuilder<TContext> UseMySql<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, IConfiguration config,
        object mySqlOptionsAction = null, object serverVersion = null)
        where TContext : DbContext
    {
        ArgumentGuard.NotNull(optionsBuilder);
        ArgumentGuard.NotNull(config);

        string connection = GetConnection(config);

        return DoUseMySql(optionsBuilder, connection, mySqlOptionsAction, serverVersion);
    }

    /// <summary>
    /// Configure Entity Framework Core to use a MySQL database identified by a named service binding.
    /// </summary>
    /// <typeparam name="TContext">
    /// Type of <see cref="DbContext" />.
    /// </typeparam>
    /// <param name="optionsBuilder">
    /// <see cref="DbContextOptionsBuilder" />.
    /// </param>
    /// <param name="config">
    /// Application configuration.
    /// </param>
    /// <param name="serviceName">
    /// The name of the service binding to use.
    /// </param>
    /// <param name="mySqlOptionsAction">
    /// An action for customizing the MySqlDbContextOptionsBuilder.
    /// </param>
    /// <param name="serverVersion">
    /// The version of MySQL/MariaDB to connect to (introduced in EF Core 5.0).
    /// </param>
    /// <returns>
    /// <see cref="DbContextOptionsBuilder" />, configured to use MySQL.
    /// </returns>
    public static DbContextOptionsBuilder<TContext> UseMySql<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, IConfiguration config,
        string serviceName, object mySqlOptionsAction = null, object serverVersion = null)
        where TContext : DbContext
    {
        ArgumentGuard.NotNull(optionsBuilder);
        ArgumentGuard.NotNull(config);

        if (string.IsNullOrEmpty(serviceName))
        {
            throw new ArgumentException(nameof(serviceName));
        }

        string connection = GetConnection(config, serviceName);

        return DoUseMySql(optionsBuilder, connection, mySqlOptionsAction, serverVersion);
    }

    private static string GetConnection(IConfiguration config, string serviceName = null)
    {
        MySqlServiceInfo info = string.IsNullOrEmpty(serviceName)
            ? config.GetSingletonServiceInfo<MySqlServiceInfo>()
            : config.GetRequiredServiceInfo<MySqlServiceInfo>(serviceName);

        var mySqlConfig = new MySqlProviderConnectorOptions(config);

        var factory = new MySqlProviderConnectorFactory(info, mySqlConfig, null);

        return factory.CreateConnectionString();
    }

    private static DbContextOptionsBuilder DoUseMySql(DbContextOptionsBuilder builder, string connection, object mySqlOptionsAction = null,
        object serverVersion = null)
    {
        Type extensionType = EntityFrameworkCoreTypeLocator.MySqlDbContextOptionsType;

        MethodInfo useMethod = null;

        object[] parameters =
        {
        };

        // In Pomelo requires server version but MySql.Data does not. If the type is defined, make sure we have a value and use a compatible method
        if (EntityFrameworkCoreTypeLocator.MySqlVersionType != null)
        {
            useMethod = FindUseSqlMethod(extensionType, new[]
            {
                typeof(DbContextOptionsBuilder),
                typeof(string),
                EntityFrameworkCoreTypeLocator.MySqlVersionType,
                typeof(Action<DbContextOptionsBuilder>)
            });

            // If the server version wasn't passed in, see if we need to use the EF Core lib to autodetect it (this is the part that creates an extra connection)
            serverVersion ??= ReflectionHelpers.FindMethod(EntityFrameworkCoreTypeLocator.MySqlVersionType, "AutoDetect", new[]
            {
                typeof(string)
            }).Invoke(null, new object[]
            {
                connection
            });

            parameters = new[]
            {
                builder,
                connection,
                serverVersion,
                mySqlOptionsAction
            };
        }

        if (useMethod == null)
        {
            useMethod = FindUseSqlMethod(extensionType, new[]
            {
                typeof(DbContextOptionsBuilder),
                typeof(string),
                typeof(Action<DbContextOptionsBuilder>)
            });

            parameters = new[]
            {
                builder,
                connection,
                mySqlOptionsAction
            };
        }

        if (extensionType == null)
        {
            throw new ConnectorException("Unable to find UseMySql extension, are you missing MySql EntityFramework Core assembly");
        }

        object result = ReflectionHelpers.Invoke(useMethod, null, parameters);

        if (result == null)
        {
            throw new ConnectorException($"Failed to invoke UseMySql extension, connection: {connection}");
        }

        return (DbContextOptionsBuilder)result;
    }

    private static DbContextOptionsBuilder<TContext> DoUseMySql<TContext>(DbContextOptionsBuilder<TContext> builder, string connection,
        object mySqlOptionsAction = null, object serverVersion = null)
        where TContext : DbContext
    {
        return (DbContextOptionsBuilder<TContext>)DoUseMySql((DbContextOptionsBuilder)builder, connection, mySqlOptionsAction, serverVersion);
    }

    private static MethodInfo FindUseSqlMethod(Type type, IReadOnlyList<Type> parameterTypes)
    {
        return type.GetMethods().FirstOrDefault(method => MatchesSignature(method, parameterTypes));
    }

    private static bool MatchesSignature(MethodBase method, IReadOnlyList<Type> parameterTypes)
    {
        if (method.IsPublic && method.IsStatic && method.Name.Equals("UseMySQL", StringComparison.InvariantCultureIgnoreCase))
        {
            ParameterInfo[] parameters = method.GetParameters();

            if (parameters.Length != parameterTypes.Count)
            {
                return false;
            }

            return parameters.Length >= 2 && parameters[0].ParameterType == parameterTypes[0] && parameters[1].ParameterType == parameterTypes[1];
        }

        return false;
    }
}
