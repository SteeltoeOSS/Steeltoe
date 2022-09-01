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

namespace Steeltoe.Connector.Oracle.EntityFrameworkCore;

public static class OracleDbContextOptionsExtensions
{
    public static DbContextOptionsBuilder UseOracle(this DbContextOptionsBuilder optionsBuilder, IConfiguration configuration,
        object oracleOptionsAction = null)
    {
        ArgumentGuard.NotNull(optionsBuilder);
        ArgumentGuard.NotNull(configuration);

        string connection = GetConnection(configuration);

        return DoUseOracle(optionsBuilder, connection, oracleOptionsAction);
    }

    public static DbContextOptionsBuilder UseOracle(this DbContextOptionsBuilder optionsBuilder, IConfiguration configuration, string serviceName,
        object oracleOptionsAction = null)
    {
        ArgumentGuard.NotNull(optionsBuilder);
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNullOrEmpty(serviceName);

        string connection = GetConnection(configuration, serviceName);

        return DoUseOracle(optionsBuilder, connection, oracleOptionsAction);
    }

    public static DbContextOptionsBuilder<TContext> UseOracle<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, IConfiguration configuration,
        object oracleOptionsAction = null)
        where TContext : DbContext
    {
        ArgumentGuard.NotNull(optionsBuilder);
        ArgumentGuard.NotNull(configuration);

        string connection = GetConnection(configuration);

        return DoUseOracle(optionsBuilder, connection, oracleOptionsAction);
    }

    public static DbContextOptionsBuilder<TContext> UseOracle<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, IConfiguration configuration,
        string serviceName, object oracleOptionsAction = null)
        where TContext : DbContext
    {
        ArgumentGuard.NotNull(optionsBuilder);
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNullOrEmpty(serviceName);

        string connection = GetConnection(configuration, serviceName);

        return DoUseOracle(optionsBuilder, connection, oracleOptionsAction);
    }

    private static DbContextOptionsBuilder DoUseOracle(DbContextOptionsBuilder optionsBuilder, object connection, object oracleOptionsAction)
    {
        Type extensionType = EntityFrameworkCoreTypeLocator.OracleDbContextOptionsType;

        MethodInfo useMethod = FindUseSqlMethod(extensionType, new[]
        {
            typeof(DbContextOptionsBuilder),
            typeof(string)
        });

        if (extensionType == null)
        {
            throw new ConnectorException("Unable to find UseOracle extension, are you missing Oracle EntityFramework Core assembly");
        }

        object result = ReflectionHelpers.Invoke(useMethod, null, new[]
        {
            optionsBuilder,
            connection,
            oracleOptionsAction
        });

        if (result == null)
        {
            throw new ConnectorException($"Failed to invoke UseOracle extension, connection: {connection}");
        }

        return (DbContextOptionsBuilder)result;
    }

    private static DbContextOptionsBuilder<TContext> DoUseOracle<TContext>(DbContextOptionsBuilder<TContext> optionsBuilder, string connection,
        object oracleOptionsAction = null)
        where TContext : DbContext
    {
        return (DbContextOptionsBuilder<TContext>)DoUseOracle((DbContextOptionsBuilder)optionsBuilder, connection, oracleOptionsAction);
    }

    private static MethodInfo FindUseSqlMethod(Type type, IReadOnlyList<Type> parameterTypes)
    {
        return type.GetMethods().FirstOrDefault(method => MatchesSignature(method, parameterTypes));
    }

    private static bool MatchesSignature(MethodBase method, IReadOnlyList<Type> parameterTypes)
    {
        if (method.IsPublic && method.IsStatic && method.Name.Equals("UseOracle", StringComparison.OrdinalIgnoreCase))
        {
            ParameterInfo[] parameters = method.GetParameters();
            return parameters.Length == 3 && parameters[0].ParameterType == parameterTypes[0] && parameters[1].ParameterType == parameterTypes[1];
        }

        return false;
    }

    private static string GetConnection(IConfiguration configuration, string serviceName = null)
    {
        OracleServiceInfo info = string.IsNullOrEmpty(serviceName)
            ? configuration.GetSingletonServiceInfo<OracleServiceInfo>()
            : configuration.GetRequiredServiceInfo<OracleServiceInfo>(serviceName);

        var options = new OracleProviderConnectorOptions(configuration);

        var factory = new OracleProviderConnectorFactory(info, options, null);

        return factory.CreateConnectionString();
    }
}
