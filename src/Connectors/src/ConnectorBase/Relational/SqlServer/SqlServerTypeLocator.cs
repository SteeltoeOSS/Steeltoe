// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.CloudFoundry.Connector.SqlServer
{
    /// <summary>
    /// Assemblies and types used for interacting with Microsoft SQL Server
    /// </summary>
    public static class SqlServerTypeLocator
    {
        /// <summary>
        /// Gets SqlConnection from a SQL Server Library
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type SqlConnection => ConnectorHelpers.FindTypeOrThrow(Assemblies, ConnectionTypeNames, "SqlConnection", "a Microsoft SQL Server ADO.NET assembly");

        /// <summary>
        /// Gets the list of supported SQL Server Client assemblies
        /// </summary>
        public static string[] Assemblies { get; internal set; } = new string[] { "System.Data.SqlClient" };

        /// <summary>
        /// Gets the list of SQL Server types that implement IDbConnection
        /// </summary>
        public static string[] ConnectionTypeNames { get; internal set; } = new string[] { "System.Data.SqlClient.SqlConnection" };
    }
}

#pragma warning disable SA1403 // File may only contain a single namespace
namespace Steeltoe.CloudFoundry.Connector.Relational.SqlServer
#pragma warning restore SA1403 // File may only contain a single namespace
{
#pragma warning disable SA1402 // File may only contain a single class
    /// <summary>
    /// Assemblies and types used for interacting with Microsoft SQL Server
    /// </summary>
    [Obsolete("The namespace of this class is changing to 'Steeltoe.CloudFoundry.Connector.SqlServer'")]
    public static class SqlServerTypeLocator
#pragma warning restore SA1402 // File may only contain a single class
    {
        /// <summary>
        /// List of supported SQL Server Client assemblies
        /// </summary>
        public static readonly string[] Assemblies = new string[] { "System.Data.SqlClient" };

        /// <summary>
        /// List of SQL Server types that implement IDbConnection
        /// </summary>
        public static readonly string[] ConnectionTypeNames = new string[] { "System.Data.SqlClient.SqlConnection" };

        /// <summary>
        /// Gets SqlConnection from a SQL Server Library
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type SqlConnection
        {
            get
            {
                var type = ConnectorHelpers.FindType(Assemblies, ConnectionTypeNames);
                if (type == null)
                {
#pragma warning disable S2372 // Exceptions should not be thrown from property getters
                    throw new ConnectorException("Unable to find SqlConnection, are you missing a Microsoft SQL Server ADO.NET assembly?");
#pragma warning restore S2372 // Exceptions should not be thrown from property getters
                }

                return type;
            }
        }
    }
}
