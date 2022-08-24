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

namespace Steeltoe.Connector.SqlServer.EntityFrameworkCore;

public static class SqlServerDbContextOptionsExtensions
{
    public static DbContextOptionsBuilder UseSqlServer(this DbContextOptionsBuilder optionsBuilder, IConfiguration configuration,
        object sqlServerOptionsAction = null)
    {
        ArgumentGuard.NotNull(optionsBuilder);
        ArgumentGuard.NotNull(configuration);

        string connection = GetConnection(configuration);

        return DoUseSqlServer(optionsBuilder, connection, sqlServerOptionsAction);
    }

    public static DbContextOptionsBuilder UseSqlServer(this DbContextOptionsBuilder optionsBuilder, IConfiguration configuration, string serviceName,
        object sqlServerOptionsAction = null)
    {
        ArgumentGuard.NotNull(optionsBuilder);
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNullOrEmpty(serviceName);

        string connection = GetConnection(configuration, serviceName);

        return DoUseSqlServer(optionsBuilder, connection, sqlServerOptionsAction);
    }

    public static DbContextOptionsBuilder<TContext> UseSqlServer<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, IConfiguration configuration,
        object sqlServerOptionsAction = null)
        where TContext : DbContext
    {
        ArgumentGuard.NotNull(optionsBuilder);
        ArgumentGuard.NotNull(configuration);

        string connection = GetConnection(configuration);

        return DoUseSqlServer(optionsBuilder, connection, sqlServerOptionsAction);
    }

    public static DbContextOptionsBuilder<TContext> UseSqlServer<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder, IConfiguration configuration,
        string serviceName, object sqlServerOptionsAction = null)
        where TContext : DbContext
    {
        ArgumentGuard.NotNull(optionsBuilder);
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNullOrEmpty(serviceName);

        string connection = GetConnection(configuration, serviceName);

        return DoUseSqlServer(optionsBuilder, connection, sqlServerOptionsAction);
    }

    private static MethodInfo FindUseSqlMethod(Type type, IReadOnlyList<Type> parameterTypes)
    {
        return type.GetMethods().FirstOrDefault(method => MatchesSignature(parameterTypes, method));
    }

    private static bool MatchesSignature(IReadOnlyList<Type> parameterTypes, MethodInfo method)
    {
        if (method.IsPublic && method.IsStatic && method.Name.Equals("UseSqlServer", StringComparison.OrdinalIgnoreCase))
        {
            ParameterInfo[] parameters = method.GetParameters();
            return parameters.Length == 3 && parameters[0].ParameterType == parameterTypes[0] && parameters[1].ParameterType == parameterTypes[1];
        }

        return false;
    }

    private static string GetConnection(IConfiguration configuration, string serviceName = null)
    {
        SqlServerServiceInfo info = string.IsNullOrEmpty(serviceName)
            ? configuration.GetSingletonServiceInfo<SqlServerServiceInfo>()
            : configuration.GetRequiredServiceInfo<SqlServerServiceInfo>(serviceName);

        var options = new SqlServerProviderConnectorOptions(configuration);

        var factory = new SqlServerProviderConnectorFactory(info, options, null);

        return factory.CreateConnectionString();
    }

    private static DbContextOptionsBuilder DoUseSqlServer(DbContextOptionsBuilder builder, string connection, object sqlServerOptionsAction = null)
    {
        Type extensionType = EntityFrameworkCoreTypeLocator.SqlServerDbContextOptionsType;

        MethodInfo useMethod = FindUseSqlMethod(extensionType, new[]
        {
            typeof(DbContextOptionsBuilder),
            typeof(string)
        });

        if (extensionType == null)
        {
            throw new ConnectorException("Unable to find UseSqlServer extension, are you missing SqlServer EntityFramework Core assembly");
        }

        object result = ReflectionHelpers.Invoke(useMethod, null, new[]
        {
            builder,
            connection,
            sqlServerOptionsAction
        });

        if (result == null)
        {
            throw new ConnectorException($"Failed to invoke UseSqlServer extension, connection: {connection}");
        }

        return (DbContextOptionsBuilder)result;
    }

    private static DbContextOptionsBuilder<TContext> DoUseSqlServer<TContext>(DbContextOptionsBuilder<TContext> builder, string connection,
        object sqlServerOptionsAction = null)
        where TContext : DbContext
    {
        return (DbContextOptionsBuilder<TContext>)DoUseSqlServer((DbContextOptionsBuilder)builder, connection, sqlServerOptionsAction);
    }
}
