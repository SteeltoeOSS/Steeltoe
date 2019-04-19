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

using System;

namespace Steeltoe.CloudFoundry.Connector.EFCore
{
    /// <summary>
    /// Assemblies and types for interacting with Entity Framework Core
    /// </summary>
    public static class EntityFrameworkCoreTypeLocator
    {
        /// <summary>
        /// List of supported MySQL Entity Framework Core Assemblies
        /// </summary>
        public static string[] MySqlEntityAssemblies = new string[] { "MySql.Data.EntityFrameworkCore", "Pomelo.EntityFrameworkCore.MySql" };

        /// <summary>
        /// List of supported fully-qualifed names for compatible DbContextOptionsExtentions used to configure EntityFrameworkCore
        /// </summary>
        public static string[] MySqlEntityTypeNames = new string[] { "MySQL.Data.EntityFrameworkCore.Extensions.MySQLDbContextOptionsExtensions", "Microsoft.EntityFrameworkCore.MySqlDbContextOptionsExtensions", "Microsoft.EntityFrameworkCore.MySQLDbContextOptionsExtensions" };

        /// <summary>
        /// Gets the type used to configure EntityFramework Core with MySql
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type MySqlDbContextOptionsType => ConnectorHelpers.FindTypeOrThrow(MySqlEntityAssemblies, MySqlEntityTypeNames, "DbContextOptionsBuilder", "a MySql EntityFramework Core assembly");

        /// <summary>
        /// List of supported PostgreSQL Entity Framework Core Assemblies
        /// </summary>
        public static string[] PostgreSqlEntityAssemblies = new string[] { "Npgsql.EntityFrameworkCore.PostgreSQL" };

        /// <summary>
        /// List of supported fully-qualifed names for compatible DbContextOptionsExtentions used to configure EntityFrameworkCore
        /// </summary>
        public static string[] PostgreSqlEntityTypeNames = new string[] { "Microsoft.EntityFrameworkCore.NpgsqlDbContextOptionsExtensions" };

        /// <summary>
        /// Gets the type used to configure EntityFramework Core with PostgreSQL
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type PostgreSqlDbContextOptionsType => ConnectorHelpers.FindTypeOrThrow(PostgreSqlEntityAssemblies, PostgreSqlEntityTypeNames, "DbContextOptionsBuilder", "a PostgreSql EntityFramework Core assembly");

        /// <summary>
        /// List of supported Microsoft SQL Server Entity Framework Core Assemblies
        /// </summary>
        public static string[] SqlServerEntityAssemblies = new string[] { "Microsoft.EntityFrameworkCore.SqlServer" };

        /// <summary>
        /// List of supported fully-qualifed names for compatible DbContextOptionsExtentions used to configure EntityFrameworkCore
        /// </summary>
        public static string[] SqlServerEntityTypeNames = new string[] { "Microsoft.EntityFrameworkCore.SqlServerDbContextOptionsExtensions" };

        /// <summary>
        /// Gets the type used to configure EntityFramework Core with Microsoft SQL Server
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type SqlServerDbContextOptionsType => ConnectorHelpers.FindTypeOrThrow(SqlServerEntityAssemblies, SqlServerEntityTypeNames, "DbContextOptionsBuilder", "a Microsoft SQL Server EntityFramework Core assembly");
    }
}
