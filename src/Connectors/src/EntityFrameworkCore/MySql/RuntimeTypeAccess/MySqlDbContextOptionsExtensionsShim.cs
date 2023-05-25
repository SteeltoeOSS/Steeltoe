// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.EntityFrameworkCore;
using Steeltoe.Common;

namespace Steeltoe.Connectors.EntityFrameworkCore.MySql.RuntimeTypeAccess;

internal static class MySqlDbContextOptionsExtensionsShim
{
    public static void UseMySql(MySqlEntityFrameworkCorePackageResolver packageResolver, DbContextOptionsBuilder optionsBuilder, string? connectionString,
        object? serverVersion, object? mySqlOptionsAction)
    {
        ArgumentGuard.NotNull(packageResolver);

        bool isOraclePackage = packageResolver.MySqlDbContextOptionsExtensionsClass.Type.Name.StartsWith("MySQL", StringComparison.Ordinal);

        if (isOraclePackage)
        {
            UseOracleMySql(packageResolver, optionsBuilder, connectionString, mySqlOptionsAction);
        }
        else
        {
            UsePomeloMySql(packageResolver, optionsBuilder, connectionString, serverVersion, mySqlOptionsAction);
        }
    }

    private static void UseOracleMySql(MySqlEntityFrameworkCorePackageResolver packageResolver, DbContextOptionsBuilder optionsBuilder,
        string? connectionString, object? mySqlOptionsAction)
    {
        Type mySqlOptionsActionType = typeof(Action<>).MakeGenericType(packageResolver.MySqlDbContextOptionsBuilderClass.Type);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // The overload that takes a connectionString parameter throws when it is null, empty or whitespace.
            _ = packageResolver.MySqlDbContextOptionsExtensionsClass.InvokeMethodOverload("UseMySQL", true, new[]
            {
                typeof(DbContextOptionsBuilder),
                mySqlOptionsActionType
            }, optionsBuilder, mySqlOptionsAction);
        }
        else
        {
            _ = packageResolver.MySqlDbContextOptionsExtensionsClass.InvokeMethodOverload("UseMySQL", true, new[]
            {
                typeof(DbContextOptionsBuilder),
                typeof(string),
                mySqlOptionsActionType
            }, optionsBuilder, connectionString, mySqlOptionsAction);
        }
    }

    private static void UsePomeloMySql(MySqlEntityFrameworkCorePackageResolver packageResolver, DbContextOptionsBuilder optionsBuilder,
        string? connectionString, object? serverVersion, object? mySqlOptionsAction)
    {
        if (serverVersion == null)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Server version must be specified when no connection string is provided.");
            }

            // Pomelo requires to specify server version. If not provided, autodetect it (this is the part that creates an extra connection).
            ServerVersionShim serverVersionShim = ServerVersionShim.AutoDetect(packageResolver, connectionString);
            serverVersion = serverVersionShim.Instance;
        }

        Type mySqlOptionsActionType = typeof(Action<>).MakeGenericType(packageResolver.MySqlDbContextOptionsBuilderClass.Type);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // The overload that takes a connectionString parameter throws when it is null, empty or whitespace.
            _ = packageResolver.MySqlDbContextOptionsExtensionsClass.InvokeMethodOverload("UseMySql", true, new[]
            {
                typeof(DbContextOptionsBuilder),
                packageResolver.ServerVersionClass.Type,
                mySqlOptionsActionType
            }, optionsBuilder, serverVersion, mySqlOptionsAction);
        }
        else
        {
            _ = packageResolver.MySqlDbContextOptionsExtensionsClass.InvokeMethodOverload("UseMySql", true, new[]
            {
                typeof(DbContextOptionsBuilder),
                typeof(string),
                packageResolver.ServerVersionClass.Type,
                mySqlOptionsActionType
            }, optionsBuilder, connectionString, serverVersion, mySqlOptionsAction);
        }
    }
}
