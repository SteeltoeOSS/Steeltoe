// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using Steeltoe.Common.Reflection;

// ReSharper disable once CheckNamespace
namespace Steeltoe.Connectors.MySql;

/// <summary>
/// Assemblies and types used for interacting with MySQL.
/// </summary>
public static class MySqlTypeLocator
{
    /// <summary>
    /// Gets a list of supported MySQL assemblies.
    /// </summary>
    public static string[] Assemblies { get; internal set; } =
    {
        "MySql.Data",
        "MySqlConnector"
    };

    /// <summary>
    /// Gets a list of MySQL types that implement <see cref="DbConnection" />.
    /// </summary>
    public static string[] ConnectionTypeNames { get; internal set; } =
    {
        "MySql.Data.MySqlClient.MySqlConnection",
        "MySqlConnector.MySqlConnection"
    };

    /// <summary>
    /// Gets MySqlConnection type from a MySQL Library.
    /// </summary>
    public static Type MySqlConnection => ReflectionHelpers.FindTypeOrThrow(Assemblies, ConnectionTypeNames, "MySqlConnection", "a MySQL ADO.NET assembly");
}
