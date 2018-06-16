// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.CloudFoundry.Connector;
using System;

namespace Steeltoe.CloudFoundry.Connector.Relational.PostgreSql
{
    /// <summary>
    /// Assemblies and types used for interacting with PostgreSQL
    /// </summary>
    public static class PostgreSqlTypeLocator
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
        public static Type NpgsqlConnection
        {
            get
            {
                var type = ConnectorHelpers.FindType(Assemblies, ConnectionTypeNames);
                if (type == null)
                {
                    throw new ConnectorException("Unable to find NpgsqlConnection, are you missing a PostgreSQL ADO.NET assembly?");
                }

                return type;
            }
        }
    }
}
