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

namespace Steeltoe.Connector.PostgreSql.EFCore;

public static class PostgresDbContextOptionsExtensions
{
    public static DbContextOptionsBuilder UseNpgsql(this DbContextOptionsBuilder optionsBuilder, IConfiguration config, object npgsqlOptionsAction = null)
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

        return DoUseNpgsql(optionsBuilder, connection, npgsqlOptionsAction);
    }

    public static DbContextOptionsBuilder UseNpgsql(this DbContextOptionsBuilder optionsBuilder, IConfiguration config, string serviceName, object npgsqlOptionsAction = null)
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

        return DoUseNpgsql(optionsBuilder, connection, npgsqlOptionsAction);
    }

    public static DbContextOptionsBuilder<TContext> UseNpgsql<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, IConfiguration config, object npgsqlOptionsAction = null)
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

        return DoUseNpgsql(optionsBuilder, connection, npgsqlOptionsAction);
    }

    public static DbContextOptionsBuilder<TContext> UseNpgsql<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, IConfiguration config, string serviceName, object npgsqlOptionsAction = null)
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

        return DoUseNpgsql(optionsBuilder, connection, npgsqlOptionsAction);
    }

    public static MethodInfo FindUseNpgsqlMethod(Type type, Type[] parameterTypes)
    {
        var typeInfo = type.GetTypeInfo();
        var declaredMethods = typeInfo.DeclaredMethods;

        foreach (var ci in declaredMethods)
        {
            var parameters = ci.GetParameters();

            if (parameters.Length == 3 && ci.Name.Equals("UseNpgsql") &&
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
            ? config.GetSingletonServiceInfo<PostgresServiceInfo>()
            : config.GetRequiredServiceInfo<PostgresServiceInfo>(serviceName);

        var postgresConfig = new PostgresProviderConnectorOptions(config);

        var factory = new PostgresProviderConnectorFactory(info, postgresConfig, null);
        return factory.CreateConnectionString();
    }

    private static DbContextOptionsBuilder DoUseNpgsql(DbContextOptionsBuilder builder, string connection, object npgsqlOptionsAction = null)
    {
        var extensionType = EntityFrameworkCoreTypeLocator.PostgreSqlDbContextOptionsType;

        var useMethod = FindUseNpgsqlMethod(extensionType, new Type[] { typeof(DbContextOptionsBuilder), typeof(string) });
        if (extensionType == null)
        {
            throw new ConnectorException("Unable to find UseNpgsql extension, are you missing Postgres EntityFramework Core assembly");
        }

        var result = ReflectionHelpers.Invoke(useMethod, null, new object[] { builder, connection, npgsqlOptionsAction });
        if (result == null)
        {
            throw new ConnectorException(string.Format("Failed to invoke UseNpgsql extension, connection: {0}", connection));
        }

        return (DbContextOptionsBuilder)result;
    }

    private static DbContextOptionsBuilder<TContext> DoUseNpgsql<TContext>(DbContextOptionsBuilder<TContext> builder, string connection, object npgsqlOptionsAction = null)
        where TContext : DbContext
    {
        return (DbContextOptionsBuilder<TContext>)DoUseNpgsql((DbContextOptionsBuilder)builder, connection, npgsqlOptionsAction);
    }
}