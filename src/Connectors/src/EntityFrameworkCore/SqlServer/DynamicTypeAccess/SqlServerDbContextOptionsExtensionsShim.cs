// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;

namespace Steeltoe.Connectors.EntityFrameworkCore.SqlServer.DynamicTypeAccess;

internal static class SqlServerDbContextOptionsExtensionsShim
{
    public static void UseSqlServer(SqlServerEntityFrameworkCorePackageResolver packageResolver, DbContextOptionsBuilder optionsBuilder,
        string? connectionString, object? sqlServerOptionsAction = null)
    {
        ArgumentNullException.ThrowIfNull(packageResolver);

        Type sqlServerOptionsActionType = typeof(Action<>).MakeGenericType(packageResolver.SqlServerDbContextOptionsBuilderClass.Type);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // The overload that takes a connectionString parameter throws when it is null, empty or whitespace.
            _ = packageResolver.SqlServerDbContextOptionsExtensionsClass.InvokeMethodOverload("UseSqlServer", true, [
                typeof(DbContextOptionsBuilder),
                sqlServerOptionsActionType
            ], optionsBuilder, sqlServerOptionsAction);
        }
        else
        {
            _ = packageResolver.SqlServerDbContextOptionsExtensionsClass.InvokeMethodOverload("UseSqlServer", true, [
                typeof(DbContextOptionsBuilder),
                typeof(string),
                sqlServerOptionsActionType
            ], optionsBuilder, connectionString, sqlServerOptionsAction);
        }
    }
}
