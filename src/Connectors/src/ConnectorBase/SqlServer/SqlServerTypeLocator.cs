// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Reflection;
using System;

namespace Steeltoe.Connector.SqlServer;

/// <summary>
/// Assemblies and types used for interacting with Microsoft SQL Server
/// </summary>
public static class SqlServerTypeLocator
{
    /// <summary>
    /// Gets SqlConnection from a SQL Server Library
    /// </summary>
    /// <exception cref="ConnectorException">When type is not found</exception>
    public static Type SqlConnection => ReflectionHelpers.FindTypeOrThrow(Assemblies, ConnectionTypeNames, "SqlConnection", "a Microsoft SQL Server ADO.NET assembly");

    /// <summary>
    /// Gets the list of supported SQL Server Client assemblies
    /// </summary>
    public static string[] Assemblies { get; internal set; } = new string[] { "System.Data.SqlClient" };

    /// <summary>
    /// Gets the list of SQL Server types that implement IDbConnection
    /// </summary>
    public static string[] ConnectionTypeNames { get; internal set; } = new string[] { "System.Data.SqlClient.SqlConnection" };
}