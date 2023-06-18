// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using Steeltoe.Common.Reflection;

// ReSharper disable once CheckNamespace
namespace Steeltoe.Connectors.PostgreSql;

/// <summary>
/// Assemblies and types used for interacting with PostgreSQL.
/// </summary>
public static class PostgreSqlTypeLocator
{
    /// <summary>
    /// Gets a list of supported PostgreSQL assemblies.
    /// </summary>
    public static string[] Assemblies { get; internal set; } =
    {
        "Npgsql"
    };

    /// <summary>
    /// Gets a list of PostgreSQL types that implement <see cref="DbConnection" />.
    /// </summary>
    public static string[] ConnectionTypeNames { get; internal set; } =
    {
        "Npgsql.NpgsqlConnection"
    };

    /// <summary>
    /// Gets a list of PostgreSQL types that implement <see cref="DbConnectionStringBuilder" />.
    /// </summary>
    public static string[] ConnectionStringBuilderTypeNames { get; internal set; } =
    {
        "Npgsql.NpgsqlConnectionStringBuilder"
    };

    /// <summary>
    /// Gets NpgsqlConnection type from a PostgreSQL Library.
    /// </summary>
    public static Type NpgsqlConnection =>
        ReflectionHelpers.FindTypeOrThrow(Assemblies, ConnectionTypeNames, "NpgsqlConnection", "a PostgreSQL ADO.NET assembly");

    /// <summary>
    /// Gets NpgsqlConnectionStringBuilder type from a PostgreSQL Library.
    /// </summary>
    public static Type NpgsqlConnectionStringBuilderType =>
        ReflectionHelpers.FindTypeOrThrow(Assemblies, ConnectionStringBuilderTypeNames, "NpgsqlConnectionStringBuilder", "a PostgreSQL ADO.NET assembly");
}
