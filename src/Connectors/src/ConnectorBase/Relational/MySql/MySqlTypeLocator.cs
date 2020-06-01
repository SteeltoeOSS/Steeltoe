// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.CloudFoundry.Connector.MySql
{
    /// <summary>
    /// Assemblies and types used for interacting with MySQL
    /// </summary>
    public static class MySqlTypeLocator
    {
        /// <summary>
        /// Gets a list of supported MySQL assemblies
        /// </summary>
        public static string[] Assemblies { get; internal set; } = new string[] { "MySql.Data", "MySqlConnector" };

        /// <summary>
        /// Gets a list of MySQL types that implement IDbConnection
        /// </summary>
        public static string[] ConnectionTypeNames { get; internal set; } = new string[] { "MySql.Data.MySqlClient.MySqlConnection" };

        /// <summary>
        /// Gets MySqlConnection from a MySQL Library
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type MySqlConnection => ConnectorHelpers.FindTypeOrThrow(Assemblies, ConnectionTypeNames, "MySqlConnection", "a MySql ADO.NET assembly");
    }
}

#pragma warning disable SA1403 // File may only contain a single namespace
namespace Steeltoe.CloudFoundry.Connector.Relational.MySql
#pragma warning restore SA1403 // File may only contain a single namespace
{
#pragma warning disable SA1402 // File may only contain a single class
    /// <summary>
    /// Assemblies and types used for interacting with MySQL
    /// </summary>
    [Obsolete("The namespace of this class is changing to 'Steeltoe.CloudFoundry.Connector.MySql'")]
    public static class MySqlTypeLocator
    {
        /// <summary>
        /// Gets a list of supported MySQL assemblies
        /// </summary>
        public static string[] Assemblies { get; internal set; } = new string[] { "MySql.Data", "MySqlConnector" };

        /// <summary>
        /// Gets a list of MySQL types that implement IDbConnection
        /// </summary>
        public static string[] ConnectionTypeNames { get; internal set; } = new string[] { "MySql.Data.MySqlClient.MySqlConnection" };

        /// <summary>
        /// Gets MySqlConnection from a MySQL Library
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type MySqlConnection => ConnectorHelpers.FindTypeOrThrow(Assemblies, ConnectionTypeNames, "MySqlConnection", "a MySql ADO.NET assembly");
    }
#pragma warning restore SA1402 // File may only contain a single class
}
