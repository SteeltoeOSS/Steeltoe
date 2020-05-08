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

namespace Steeltoe.Connector.Oracle
{
    /// <summary>
    /// Assemblies and types used for interacting with Oracle
    /// </summary>
    public static class OracleTypeLocator
    {
        /// <summary>
        /// Gets a list of supported Oracle Client assemblies
        /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays
        public static string[] Assemblies { get; internal set; } = new string[] { "Oracle.ManagedDataAccess" };

        /// <summary>
        /// Gets a list of Oracle types that implement IDbConnection
        /// </summary>
        public static string[] ConnectionTypeNames { get; internal set; } = new string[] { "Oracle.ManagedDataAccess.Client.OracleConnection" };
#pragma warning restore CA1819 // Properties should not return arrays

        /// <summary>
        /// Gets SqlConnection from a Oracle Library
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type OracleConnection => ReflectionHelpers.FindTypeOrThrow(Assemblies, ConnectionTypeNames, "OracleConnection", "a Oracle ODP.NET assembly");
    }
}
