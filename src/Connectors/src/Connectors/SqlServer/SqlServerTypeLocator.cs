// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using Steeltoe.Common.Reflection;

// ReSharper disable once CheckNamespace
namespace Steeltoe.Connectors.SqlServer;

/// <summary>
/// Assemblies and types used for interacting with Microsoft SQL Server.
/// </summary>
public static class SqlServerTypeLocator
{
    /// <summary>
    /// Gets the list of supported SQL Server Client assemblies.
    /// </summary>
    public static string[] Assemblies { get; internal set; } =
    {
        "Microsoft.Data.SqlClient",
        "System.Data.SqlClient"
    };

    /// <summary>
    /// Gets the list of SQL Server types that implement <see cref="DbConnection" />.
    /// </summary>
    public static string[] ConnectionTypeNames { get; internal set; } =
    {
        "Microsoft.Data.SqlClient.SqlConnection",
        "System.Data.SqlClient.SqlConnection"
    };

    /// <summary>
    /// Gets a list of SQL Server types that implement <see cref="DbConnectionStringBuilder" />.
    /// </summary>
    public static string[] ConnectionStringBuilderTypeNames { get; internal set; } =
    {
        "Microsoft.Data.SqlClient.SqlConnectionStringBuilder",
        "System.Data.SqlClient.SqlConnectionStringBuilder"
    };

    /// <summary>
    /// Gets SqlConnection type from a SQL Server Library.
    /// </summary>
    public static Type SqlConnection =>
        ReflectionHelpers.FindTypeOrThrow(Assemblies, ConnectionTypeNames, "SqlConnection", "a Microsoft SQL Server ADO.NET assembly");

    /// <summary>
    /// Gets SqlConnectionStringBuilder type from a SQL Server Library.
    /// </summary>
    public static Type SqlConnectionStringBuilderType =>
        ReflectionHelpers.FindTypeOrThrow(Assemblies, ConnectionStringBuilderTypeNames, "SqlConnectionStringBuilder",
            "a Microsoft SQL Server ADO.NET assembly");
}
