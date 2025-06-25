// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.MySql.DynamicTypeAccess;

/// <summary>
/// Provides access to types in MySQL NuGet packages, without referencing them.
/// </summary>
internal sealed class MySqlPackageResolver : PackageResolver
{
    private static readonly (string AssemblyName, string PackageName) MySqlConnector = new("MySqlConnector", "MySqlConnector");
    private static readonly (string AssemblyName, string PackageName) Oracle = new("MySql.Data", "MySql.Data");

    internal static readonly MySqlPackageResolver MySqlConnectorOnly = new(MySqlConnector.AssemblyName, MySqlConnector.PackageName);
    internal static readonly MySqlPackageResolver OracleOnly = new(Oracle.AssemblyName, Oracle.PackageName);

    public static readonly MySqlPackageResolver Default = new([
        MySqlConnector.AssemblyName,
        Oracle.AssemblyName
    ], [
        MySqlConnector.PackageName,
        Oracle.PackageName
    ]);

    public TypeAccessor MySqlConnectionStringBuilderClass =>
        ResolveType("MySqlConnector.MySqlConnectionStringBuilder", "MySql.Data.MySqlClient.MySqlConnectionStringBuilder");

    public TypeAccessor MySqlConnectionClass => ResolveType("MySqlConnector.MySqlConnection", "MySql.Data.MySqlClient.MySqlConnection");

    private MySqlPackageResolver(string assemblyName, string packageName)
        : base(assemblyName, packageName)
    {
    }

    private MySqlPackageResolver(IReadOnlyList<string> assemblyNames, IReadOnlyList<string> packageNames)
        : base(assemblyNames, packageNames)
    {
    }
}
