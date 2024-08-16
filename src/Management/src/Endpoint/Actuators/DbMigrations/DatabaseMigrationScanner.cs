// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.Management.Endpoint.Actuators.DbMigrations;

internal sealed class DatabaseMigrationScanner : IDatabaseMigrationScanner
{
    private static readonly Type? MigrationsExtensionsType =
        Type.GetType("Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions,Microsoft.EntityFrameworkCore.Relational");

    internal static readonly Type? DbContextType = Type.GetType("Microsoft.EntityFrameworkCore.DbContext, Microsoft.EntityFrameworkCore");

    internal static readonly MethodInfo? GetDatabaseMethod = DbContextType?.GetProperty("Database", BindingFlags.Public | BindingFlags.Instance)?.GetMethod;

    internal static readonly MethodInfo? GetPendingMigrationsMethod =
        MigrationsExtensionsType?.GetMethod("GetPendingMigrations", BindingFlags.Static | BindingFlags.Public);

    internal static readonly MethodInfo? GetAppliedMigrationsMethod =
        MigrationsExtensionsType?.GetMethod("GetAppliedMigrations", BindingFlags.Static | BindingFlags.Public);

    internal static readonly MethodInfo? GetMigrationsMethod = MigrationsExtensionsType?.GetMethod("GetMigrations", BindingFlags.Static | BindingFlags.Public);

    public Assembly AssemblyToScan => Assembly.GetEntryAssembly()!;

    public IEnumerable<string> GetPendingMigrations(object context)
    {
        return GetMigrationsReflectively(context, GetPendingMigrationsMethod);
    }

    public IEnumerable<string> GetAppliedMigrations(object context)
    {
        return GetMigrationsReflectively(context, GetAppliedMigrationsMethod);
    }

    public IEnumerable<string> GetMigrations(object context)
    {
        return GetMigrationsReflectively(context, GetMigrationsMethod);
    }

    private IEnumerable<string> GetMigrationsReflectively(object dbContext, MethodInfo? method)
    {
        if (GetDatabaseMethod == null || method == null)
        {
            return Array.Empty<string>();
        }

        object? dbFacade = GetDatabaseMethod.Invoke(dbContext, null);

        return (IEnumerable<string>)method.Invoke(null, new[]
        {
            dbFacade
        })!;
    }
}
