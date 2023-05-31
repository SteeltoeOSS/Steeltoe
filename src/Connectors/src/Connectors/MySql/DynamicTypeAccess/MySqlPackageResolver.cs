// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.MySql.DynamicTypeAccess;

/// <summary>
/// Provides access to types in MySQL NuGet packages, without referencing them.
/// </summary>
internal sealed class MySqlPackageResolver : PackageResolver
{
    private static readonly (string AssemblyName, string PackageName) MySqlConnector = new("MySqlConnector", "MySqlConnector");
    private static readonly (string AssemblyName, string PackageName) Oracle = new("MySql.Data", "MySql.Data");

    public TypeAccessor MySqlConnectionStringBuilderClass =>
        ResolveType("MySqlConnector.MySqlConnectionStringBuilder", "MySql.Data.MySqlClient.MySqlConnectionStringBuilder");

    public TypeAccessor MySqlConnectionClass => ResolveType("MySqlConnector.MySqlConnection", "MySql.Data.MySqlClient.MySqlConnection");

    public MySqlPackageResolver()
        : this(new[]
        {
            MySqlConnector.AssemblyName,
            Oracle.AssemblyName
        }, new[]
        {
            MySqlConnector.PackageName,
            Oracle.PackageName
        })
    {
    }

    private MySqlPackageResolver(IReadOnlyList<string> assemblyNames, IReadOnlyList<string> packageNames)
        : base(assemblyNames, packageNames)
    {
    }

    internal static MySqlPackageResolver CreateForOnlyMySqlConnector()
    {
        return new MySqlPackageResolver(new[]
        {
            MySqlConnector.AssemblyName
        }, new[]
        {
            MySqlConnector.PackageName
        });
    }

    internal static MySqlPackageResolver CreateForOnlyOracle()
    {
        return new MySqlPackageResolver(new[]
        {
            Oracle.AssemblyName
        }, new[]
        {
            Oracle.PackageName
        });
    }
}
