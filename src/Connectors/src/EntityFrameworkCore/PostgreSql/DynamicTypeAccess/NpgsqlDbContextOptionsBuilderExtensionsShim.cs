// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;

namespace Steeltoe.Connectors.EntityFrameworkCore.PostgreSql.DynamicTypeAccess;

internal static class NpgsqlDbContextOptionsBuilderExtensionsShim
{
    public static void UseNpgsql(PostgreSqlEntityFrameworkCorePackageResolver packageResolver, DbContextOptionsBuilder optionsBuilder, string? connectionString,
        object? npgsqlOptionsAction)
    {
        ArgumentNullException.ThrowIfNull(packageResolver);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // The overload that takes a connectionString parameter throws when it is null, empty or whitespace.
            Type npgsqlOptionsActionType = typeof(Action<>).MakeGenericType(packageResolver.NpgsqlDbContextOptionsBuilderClass.Type);

            _ = packageResolver.NpgsqlDbContextOptionsBuilderExtensionsClass.InvokeMethodOverload("UseNpgsql", true, new[]
            {
                typeof(DbContextOptionsBuilder),
                npgsqlOptionsActionType
            }, optionsBuilder, npgsqlOptionsAction);
        }
        else
        {
            Type npgsqlOptionsActionType = typeof(Action<>).MakeGenericType(packageResolver.NpgsqlDbContextOptionsBuilderClass.Type);

            _ = packageResolver.NpgsqlDbContextOptionsBuilderExtensionsClass.InvokeMethodOverload("UseNpgsql", true, new[]
            {
                typeof(DbContextOptionsBuilder),
                typeof(string),
                npgsqlOptionsActionType
            }, optionsBuilder, connectionString, npgsqlOptionsAction);
        }
    }
}
