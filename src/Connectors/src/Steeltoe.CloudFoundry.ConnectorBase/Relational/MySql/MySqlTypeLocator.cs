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

using System;

namespace Steeltoe.CloudFoundry.Connector.Relational.MySql
{
    /// <summary>
    /// Assemblies and types used for interacting with MySQL
    /// </summary>
    public static class MySqlTypeLocator
    {
        /// <summary>
        /// List of supported MySQL assemblies
        /// </summary>
        public static readonly string[] Assemblies = new string[] { "MySql.Data", "MySqlConnector" };

        /// <summary>
        /// List of MySQL types that implement IDbConnection
        /// </summary>
        public static readonly string[] ConnectionTypeNames = new string[] { "MySql.Data.MySqlClient.MySqlConnection" };

        /// <summary>
        /// Gets MySqlConnection from a MySQL Library
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type MySqlConnection
        {
            get
            {
                var type = ConnectorHelpers.FindType(Assemblies, ConnectionTypeNames);
                if (type == null)
                {
                    throw new ConnectorException("Unable to find MySqlConnection, are you missing a MySql ADO.NET assembly?");
                }

                return type;
            }
        }
    }
}
