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

namespace Steeltoe.Connector.Oracle.EFCore;

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

        return DoUseOracle(optionsBuilder, connection, oracleOptionsAction);
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

        return DoUseOracle(optionsBuilder, connection, oracleOptionsAction);
    }

    private static DbContextOptionsBuilder DoUseOracle(DbContextOptionsBuilder optionsBuilder, object connection, object oracleOptionsAction)
    {
        var extensionType = EntityFrameworkCoreTypeLocator.OracleDbContextOptionsType;

        var useMethod = FindUseSqlMethod(extensionType, new[] { typeof(DbContextOptionsBuilder), typeof(string) });
        if (extensionType == null)
        {
            throw new ConnectorException("Unable to find UseOracle extension, are you missing Oracle EntityFramework Core assembly");
        }

        var result = ReflectionHelpers.Invoke(useMethod, null, new[] { optionsBuilder, connection, oracleOptionsAction });
        if (result == null)
        {
            throw new ConnectorException($"Failed to invoke UseOracle extension, connection: {connection}");
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

        foreach (var ci in declaredMethods)
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
        var info = string.IsNullOrEmpty(serviceName)
            ? config.GetSingletonServiceInfo<OracleServiceInfo>()
            : config.GetRequiredServiceInfo<OracleServiceInfo>(serviceName);

        var oracleConfig = new OracleProviderConnectorOptions(config);

        var factory = new OracleProviderConnectorFactory(info, oracleConfig, null);

        return factory.CreateConnectionString();
    }
}
