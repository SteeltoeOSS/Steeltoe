// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Common.Reflection;
using System;

namespace Steeltoe.Connector.SqlServer
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
        public static Type SqlConnection => ReflectionHelpers.FindTypeOrThrow(Assemblies, ConnectionTypeNames, "SqlConnection", "a Microsoft SQL Server ADO.NET assembly");

        /// <summary>
        /// Gets the list of supported SQL Server Client assemblies
        /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays
        public static string[] Assemblies { get; internal set; } = new string[] { "System.Data.SqlClient" };

        /// <summary>
        /// Gets the list of SQL Server types that implement IDbConnection
        /// </summary>
        public static string[] ConnectionTypeNames { get; internal set; } = new string[] { "System.Data.SqlClient.SqlConnection" };
#pragma warning restore CA1819 // Properties should not return arrays
    }
}