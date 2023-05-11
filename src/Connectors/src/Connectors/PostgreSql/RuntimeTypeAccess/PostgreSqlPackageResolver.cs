// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Connectors.RuntimeTypeAccess;

namespace Steeltoe.Connectors.PostgreSql.RuntimeTypeAccess;

/// <summary>
/// Provides access to types in PostgreSQL NuGet packages, without referencing them.
/// </summary>
internal sealed class PostgreSqlPackageResolver : PackageResolver
{
    public TypeAccessor NpgsqlConnectionStringBuilderClass => ResolveType("Npgsql.NpgsqlConnectionStringBuilder");
    public TypeAccessor NpgsqlConnectionClass => ResolveType("Npgsql.NpgsqlConnection");

    public PostgreSqlPackageResolver()
        : base("Npgsql", "Npgsql")
    {
    }
}
