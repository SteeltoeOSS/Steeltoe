// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.EntityFrameworkCore;
using Steeltoe.Common;

namespace Steeltoe.Connectors.EntityFrameworkCore.SqlServer.RuntimeTypeAccess;

internal static class SqlServerDbContextOptionsExtensionsShim
{
    public static void UseSqlServer(SqlServerEntityFrameworkCorePackageResolver packageResolver, DbContextOptionsBuilder optionsBuilder,
        string connectionString, object? sqlServerOptionsAction = null)
    {
        ArgumentGuard.NotNull(packageResolver);

        Type sqlServerOptionsActionType = typeof(Action<>).MakeGenericType(packageResolver.SqlServerDbContextOptionsBuilderClass.Type);

        _ = packageResolver.SqlServerDbContextOptionsExtensionsClass.InvokeMethodOverload("UseSqlServer", true, new[]
        {
            typeof(DbContextOptionsBuilder),
            typeof(string),
            sqlServerOptionsActionType
        }, optionsBuilder, connectionString, sqlServerOptionsAction);
    }
}
