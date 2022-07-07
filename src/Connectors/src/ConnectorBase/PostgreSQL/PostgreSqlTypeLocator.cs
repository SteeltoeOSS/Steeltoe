// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Reflection;
using System;

namespace Steeltoe.Connector.PostgreSql;

/// <summary>
/// Assemblies and types used for interacting with PostgreSQL
/// </summary>
public static class PostgreSqlTypeLocator
{
    /// <summary>
    /// Gets a list of supported PostgreSQL assemblies
    /// </summary>
    public static string[] Assemblies { get; internal set; } = { "Npgsql" };

    /// <summary>
    /// Gets a list of PostgreSQL types that implement IDbConnection
    /// </summary>
    public static string[] ConnectionTypeNames { get; internal set; } = { "Npgsql.NpgsqlConnection" };

    /// <summary>
    /// Gets NpgsqlConnection from a PostgreSQL Library
    /// </summary>
    /// <exception cref="ConnectorException">When type is not found</exception>
    public static Type NpgsqlConnection => ReflectionHelpers.FindTypeOrThrow(Assemblies, ConnectionTypeNames, "NpgsqlConnection", "a PostgreSQL ADO.NET assembly");
}
