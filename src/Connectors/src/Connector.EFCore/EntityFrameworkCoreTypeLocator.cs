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

namespace Steeltoe.Connector.EFCore
{
    /// <summary>
    /// Assemblies and types for interacting with Entity Framework Core
    /// </summary>
    public static class EntityFrameworkCoreTypeLocator
    {
        /// <summary>
        /// Gets a list of supported MySQL Entity Framework Core Assemblies
        /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays
        public static string[] MySqlEntityAssemblies { get; internal set; } = new string[] { "MySql.Data.EntityFrameworkCore", "Pomelo.EntityFrameworkCore.MySql" };

        /// <summary>
        /// Gets a list of supported fully-qualifed names for compatible DbContextOptionsExtentions used to configure EntityFrameworkCore
        /// </summary>
        public static string[] MySqlEntityTypeNames { get; internal set; } = new string[] { "MySQL.Data.EntityFrameworkCore.Extensions.MySQLDbContextOptionsExtensions", "Microsoft.EntityFrameworkCore.MySqlDbContextOptionsExtensions", "Microsoft.EntityFrameworkCore.MySQLDbContextOptionsExtensions" };

        /// <summary>
        /// Gets the type used to configure EntityFramework Core with MySql
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type MySqlDbContextOptionsType => ReflectionHelpers.FindTypeOrThrow(MySqlEntityAssemblies, MySqlEntityTypeNames, "DbContextOptionsBuilder", "a MySql EntityFramework Core assembly");

        /// <summary>
        /// Gets a list of supported PostgreSQL Entity Framework Core Assemblies
        /// </summary>
        public static string[] PostgreSqlEntityAssemblies { get; internal set; } = new string[] { "Npgsql.EntityFrameworkCore.PostgreSQL" };

        /// <summary>
        /// Gets a list of supported fully-qualifed names for compatible DbContextOptionsExtentions used to configure EntityFrameworkCore
        /// </summary>
        public static string[] PostgreSqlEntityTypeNames { get; internal set; } = new string[] { "Microsoft.EntityFrameworkCore.NpgsqlDbContextOptionsExtensions" };

        /// <summary>
        /// Gets the type used to configure EntityFramework Core with PostgreSQL
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type PostgreSqlDbContextOptionsType => ReflectionHelpers.FindTypeOrThrow(PostgreSqlEntityAssemblies, PostgreSqlEntityTypeNames, "DbContextOptionsBuilder", "a PostgreSql EntityFramework Core assembly");

        /// <summary>
        /// Gets a list of supported Microsoft SQL Server Entity Framework Core Assemblies
        /// </summary>
        public static string[] SqlServerEntityAssemblies { get; internal set; } = new string[] { "Microsoft.EntityFrameworkCore.SqlServer" };

        /// <summary>
        /// Gets a list of supported fully-qualifed names for compatible DbContextOptionsExtentions used to configure EntityFrameworkCore
        /// </summary>
        public static string[] SqlServerEntityTypeNames { get; internal set; } = new string[] { "Microsoft.EntityFrameworkCore.SqlServerDbContextOptionsExtensions" };

        /// <summary>
        /// Gets the type used to configure EntityFramework Core with Microsoft SQL Server
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type SqlServerDbContextOptionsType => ReflectionHelpers.FindTypeOrThrow(SqlServerEntityAssemblies, SqlServerEntityTypeNames, "DbContextOptionsBuilder", "a Microsoft SQL Server EntityFramework Core assembly");

        /// <summary>
        /// Gets a list of supported Oracle Entity Framework Core Assemblies
        /// </summary>
        public static string[] OracleEntityAssemblies { get; internal set; } = new string[] { "Oracle.EntityFrameworkCore", "Devart.Data.Oracle.EFCore" };

        /// <summary>
        /// Gets a list of supported fully-qualifed names for compatible DbContextOptionsExtentions used to configure EntityFrameworkCore
        /// </summary>
        public static string[] OracleEntityTypeNames { get; internal set; } = new string[] { "Microsoft.EntityFrameworkCore.OracleDbContextOptionsExtensions", "Devart.Data.Oracle.Entity.OracleOptionsExtension" };
#pragma warning restore CA1819 // Properties should not return arrays

        /// <summary>
        /// Gets the type used to configure EntityFramework Core with Oracle
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type OracleDbContextOptionsType => ReflectionHelpers.FindTypeOrThrow(OracleEntityAssemblies, OracleEntityTypeNames, "DbContextOptionsBuilder", "a Oracle EntityFramework Core assembly");
    }
}
