// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.EntityFrameworkCore.MySql.DynamicTypeAccess;

/// <summary>
/// Provides access to types in Entity Framework Core NuGet packages for MySQL, without referencing them.
/// </summary>
internal sealed class MySqlEntityFrameworkCorePackageResolver : PackageResolver
{
    private const string PomeloPackageName = "Pomelo.EntityFrameworkCore.MySql";
    private const string OraclePackageName = "MySql.EntityFrameworkCore";

    private static readonly List<string> PomeloAssemblyNames =
    [
        "Pomelo.EntityFrameworkCore.MySql",
        "MySqlConnector"
    ];

    private static readonly List<string> OracleAssemblyNames =
    [
        "MySql.EntityFrameworkCore",
        "MySql.Data"
    ];

    public static readonly MySqlEntityFrameworkCorePackageResolver Default = new(PomeloAssemblyNames.Concat(OracleAssemblyNames).ToArray(), [
        PomeloPackageName,
        OraclePackageName
    ]);

    internal static readonly MySqlEntityFrameworkCorePackageResolver PomeloOnly = new(PomeloAssemblyNames, [PomeloPackageName]);

    internal static readonly MySqlEntityFrameworkCorePackageResolver OracleOnly = new(OracleAssemblyNames, [OraclePackageName]);

    public TypeAccessor MySqlDbContextOptionsExtensionsClass =>
        ResolveType("Microsoft.EntityFrameworkCore.MySqlDbContextOptionsBuilderExtensions", "Microsoft.EntityFrameworkCore.MySQLDbContextOptionsExtensions");

    public TypeAccessor MySqlDbContextOptionsBuilderClass =>
        ResolveType("Microsoft.EntityFrameworkCore.Infrastructure.MySqlDbContextOptionsBuilder",
            "MySql.EntityFrameworkCore.Infrastructure.MySQLDbContextOptionsBuilder");

    public TypeAccessor MySqlConnectionClass => ResolveType("MySqlConnector.MySqlConnection", "MySql.Data.MySqlClient.MySqlConnection");

    public TypeAccessor ServerVersionClass => ResolveType("Microsoft.EntityFrameworkCore.ServerVersion");

    private MySqlEntityFrameworkCorePackageResolver(IReadOnlyList<string> assemblyNames, IReadOnlyList<string> packageNames)
        : base(assemblyNames, packageNames)
    {
    }
}
