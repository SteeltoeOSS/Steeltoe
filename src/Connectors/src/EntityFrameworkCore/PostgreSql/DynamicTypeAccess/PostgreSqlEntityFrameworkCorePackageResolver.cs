// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.EntityFrameworkCore.PostgreSql.DynamicTypeAccess;

/// <summary>
/// Provides access to types in Entity Framework Core NuGet packages for PostgreSQL, without referencing them.
/// </summary>
internal sealed class PostgreSqlEntityFrameworkCorePackageResolver : PackageResolver
{
    public TypeAccessor NpgsqlDbContextOptionsBuilderExtensionsClass => ResolveType("Microsoft.EntityFrameworkCore.NpgsqlDbContextOptionsBuilderExtensions");
    public TypeAccessor NpgsqlDbContextOptionsBuilderClass => ResolveType("Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.NpgsqlDbContextOptionsBuilder");
    public TypeAccessor NpgsqlConnectionClass => ResolveType("Npgsql.NpgsqlConnection");

    public PostgreSqlEntityFrameworkCorePackageResolver()
        : base(new[]
        {
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            "Npgsql"
        }, new[]
        {
            "Npgsql.EntityFrameworkCore.PostgreSQL"
        })
    {
    }
}
