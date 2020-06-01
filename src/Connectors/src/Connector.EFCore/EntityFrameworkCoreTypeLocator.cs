// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.CloudFoundry.Connector.EFCore
{
    /// <summary>
    /// Assemblies and types for interacting with Entity Framework Core
    /// </summary>
    public static class EntityFrameworkCoreTypeLocator
    {
        /// <summary>
        /// Gets a list of supported MySQL Entity Framework Core Assemblies
        /// </summary>
        public static string[] MySqlEntityAssemblies { get; internal set; } = new string[] { "MySql.Data.EntityFrameworkCore", "Pomelo.EntityFrameworkCore.MySql" };

        /// <summary>
        /// Gets a list of supported fully-qualifed names for compatible DbContextOptionsExtentions used to configure EntityFrameworkCore
        /// </summary>
        public static string[] MySqlEntityTypeNames { get; internal set; } = new string[] { "MySQL.Data.EntityFrameworkCore.Extensions.MySQLDbContextOptionsExtensions", "Microsoft.EntityFrameworkCore.MySqlDbContextOptionsExtensions", "Microsoft.EntityFrameworkCore.MySQLDbContextOptionsExtensions" };

        /// <summary>
        /// Gets the type used to configure EntityFramework Core with MySql
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type MySqlDbContextOptionsType => ConnectorHelpers.FindTypeOrThrow(MySqlEntityAssemblies, MySqlEntityTypeNames, "DbContextOptionsBuilder", "a MySql EntityFramework Core assembly");

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
        public static Type PostgreSqlDbContextOptionsType => ConnectorHelpers.FindTypeOrThrow(PostgreSqlEntityAssemblies, PostgreSqlEntityTypeNames, "DbContextOptionsBuilder", "a PostgreSql EntityFramework Core assembly");

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
        public static Type SqlServerDbContextOptionsType => ConnectorHelpers.FindTypeOrThrow(SqlServerEntityAssemblies, SqlServerEntityTypeNames, "DbContextOptionsBuilder", "a Microsoft SQL Server EntityFramework Core assembly");

        /// <summary>
        /// Gets a list of supported Oracle Entity Framework Core Assemblies
        /// </summary>
        public static string[] OracleEntityAssemblies { get; internal set; } = new string[] { "Oracle.EntityFrameworkCore", "Devart.Data.Oracle.EFCore" };

        /// <summary>
        /// Gets a list of supported fully-qualifed names for compatible DbContextOptionsExtentions used to configure EntityFrameworkCore
        /// </summary>
        public static string[] OracleEntityTypeNames { get; internal set; } = new string[] { "Microsoft.EntityFrameworkCore.OracleDbContextOptionsExtensions", "Devart.Data.Oracle.Entity.OracleOptionsExtension" };

        /// <summary>
        /// Gets the type used to configure EntityFramework Core with Oracle
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type OracleDbContextOptionsType => ConnectorHelpers.FindTypeOrThrow(OracleEntityAssemblies, OracleEntityTypeNames, "DbContextOptionsBuilder", "a Oracle EntityFramework Core assembly");
    }
}
