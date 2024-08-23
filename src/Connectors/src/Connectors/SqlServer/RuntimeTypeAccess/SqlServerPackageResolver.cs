// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.SqlServer.RuntimeTypeAccess;

/// <summary>
/// Provides access to types in Microsoft SQL Server NuGet packages, without referencing them.
/// </summary>
internal sealed class SqlServerPackageResolver : PackageResolver
{
    private static readonly (string AssemblyName, string PackageName) MicrosoftData = new("Microsoft.Data.SqlClient", "Microsoft.Data.SqlClient");
    private static readonly (string AssemblyName, string PackageName) SystemData = new("System.Data.SqlClient", "System.Data.SqlClient");

    internal static readonly SqlServerPackageResolver MicrosoftDataOnly = new(MicrosoftData.AssemblyName, MicrosoftData.PackageName);
    internal static readonly SqlServerPackageResolver SystemDataOnly = new(SystemData.AssemblyName, SystemData.PackageName);

    public static readonly SqlServerPackageResolver Default = new([
        MicrosoftData.AssemblyName,
        SystemData.AssemblyName
    ], [
        MicrosoftData.PackageName,
        SystemData.PackageName
    ]);

    public TypeAccessor SqlConnectionStringBuilderClass =>
        ResolveType("Microsoft.Data.SqlClient.SqlConnectionStringBuilder", "System.Data.SqlClient.SqlConnectionStringBuilder");

    public TypeAccessor SqlConnectionClass => ResolveType("Microsoft.Data.SqlClient.SqlConnection", "System.Data.SqlClient.SqlConnection");

    private SqlServerPackageResolver(string assemblyName, string packageName)
        : base(assemblyName, packageName)
    {
    }

    private SqlServerPackageResolver(IReadOnlyList<string> assemblyNames, IReadOnlyList<string> packageNames)
        : base(assemblyNames, packageNames)
    {
    }
}
