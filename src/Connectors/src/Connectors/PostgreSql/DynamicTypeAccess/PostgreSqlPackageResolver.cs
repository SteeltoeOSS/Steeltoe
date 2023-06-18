// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.PostgreSql.DynamicTypeAccess;

/// <summary>
/// Provides access to types in PostgreSQL NuGet packages, without referencing them.
/// </summary>
internal sealed class PostgreSqlPackageResolver : PackageResolver
{
    public static readonly PostgreSqlPackageResolver Default = new("Npgsql", "Npgsql");

    public TypeAccessor NpgsqlConnectionStringBuilderClass => ResolveType("Npgsql.NpgsqlConnectionStringBuilder");
    public TypeAccessor NpgsqlConnectionClass => ResolveType("Npgsql.NpgsqlConnection");

    private PostgreSqlPackageResolver(string assemblyName, string packageName)
        : base(assemblyName, packageName)
    {
    }
}
