// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Reflection;

namespace Steeltoe.Connector.CosmosDb;

/// <summary>
/// Assemblies and types used for interacting with Oracle.
/// </summary>
public static class OracleTypeLocator
{
    /// <summary>
    /// Gets a list of supported Oracle Client assemblies.
    /// </summary>
    public static string[] Assemblies { get; internal set; } =
    {
        "Oracle.ManagedDataAccess"
    };

    /// <summary>
    /// Gets a list of Oracle types that implement IDbConnection.
    /// </summary>
    public static string[] ConnectionTypeNames { get; internal set; } =
    {
        "Oracle.ManagedDataAccess.Client.OracleConnection"
    };

    /// <summary>
    /// Gets SqlConnection from a Oracle Library.
    /// </summary>
    /// <exception cref="ConnectorException">
    /// When type is not found.
    /// </exception>
    public static Type OracleConnection => ReflectionHelpers.FindTypeOrThrow(Assemblies, ConnectionTypeNames, "OracleConnection", "a Oracle ODP.NET assembly");
}
