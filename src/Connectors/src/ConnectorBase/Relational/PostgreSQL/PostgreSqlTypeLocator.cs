// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.CloudFoundry.Connector.PostgreSql
{
    /// <summary>
    /// Assemblies and types used for interacting with PostgreSQL
    /// </summary>
    public static class PostgreSqlTypeLocator
    {
        /// <summary>
        /// Gets a list of supported PostgreSQL assemblies
        /// </summary>
        public static string[] Assemblies { get; internal set; } = new string[] { "Npgsql" };

        /// <summary>
        /// Gets a list of PostgreSQL types that implement IDbConnection
        /// </summary>
        public static string[] ConnectionTypeNames { get; internal set; } = new string[] { "Npgsql.NpgsqlConnection" };

        /// <summary>
        /// Gets NpgsqlConnection from a PostgreSQL Library
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type NpgsqlConnection => ConnectorHelpers.FindTypeOrThrow(Assemblies, ConnectionTypeNames, "NpgsqlConnection", "a PostgreSQL ADO.NET assembly");
    }
}

#pragma warning disable SA1403 // File may only contain a single namespace
namespace Steeltoe.CloudFoundry.Connector.Relational.PostgreSql
#pragma warning restore SA1403 // File may only contain a single namespace
{
#pragma warning disable SA1402 // File may only contain a single class
    /// <summary>
    /// Assemblies and types used for interacting with PostgreSQL
    /// </summary>
    [Obsolete("The namespace of this class is changing to 'Steeltoe.CloudFoundry.Connector.PostgreSql'")]
    public static class PostgreSqlTypeLocator
#pragma warning restore SA1402 // File may only contain a single class
    {
        /// <summary>
        /// List of supported PostgreSQL assemblies
        /// </summary>
        public static readonly string[] Assemblies = new string[] { "Npgsql" };

        /// <summary>
        /// List of PostgreSQL types that implement IDbConnection
        /// </summary>
        public static readonly string[] ConnectionTypeNames = new string[] { "Npgsql.NpgsqlConnection" };

        /// <summary>
        /// Gets NpgsqlConnection from a PostgreSQL Library
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type NpgsqlConnection => ConnectorHelpers.FindTypeOrThrow(Assemblies, ConnectionTypeNames, "NpgsqlConnection", "a PostgreSQL ADO.NET assembly");
    }
}
