// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.SqlServer.RuntimeTypeAccess;

/// <summary>
/// Provides access to types in Microsoft SQL Server NuGet packages, without referencing them.
/// </summary>
internal sealed class SqlServerPackageResolver : PackageResolver
{
    private static readonly (string AssemblyName, string PackageName) MicrosoftData = new("Microsoft.Data.SqlClient", "Microsoft.Data.SqlClient");
    private static readonly (string AssemblyName, string PackageName) SystemData = new("System.Data.SqlClient", "System.Data.SqlClient");

    public TypeAccessor SqlConnectionStringBuilderClass =>
        ResolveType("Microsoft.Data.SqlClient.SqlConnectionStringBuilder", "System.Data.SqlClient.SqlConnectionStringBuilder");

    public TypeAccessor SqlConnectionClass => ResolveType("Microsoft.Data.SqlClient.SqlConnection", "System.Data.SqlClient.SqlConnection");

    public SqlServerPackageResolver()
        : this(new[]
        {
            MicrosoftData.AssemblyName,
            SystemData.AssemblyName
        }, new[]
        {
            MicrosoftData.PackageName,
            SystemData.PackageName
        })
    {
    }

    private SqlServerPackageResolver(IReadOnlyList<string> assemblyNames, IReadOnlyList<string> packageNames)
        : base(assemblyNames, packageNames)
    {
    }

    internal static SqlServerPackageResolver CreateForOnlyMicrosoftData()
    {
        return new SqlServerPackageResolver(new[]
        {
            MicrosoftData.AssemblyName
        }, new[]
        {
            MicrosoftData.PackageName
        });
    }

    internal static SqlServerPackageResolver CreateForOnlySystemData()
    {
        return new SqlServerPackageResolver(new[]
        {
            SystemData.AssemblyName
        }, new[]
        {
            SystemData.PackageName
        });
    }
}
