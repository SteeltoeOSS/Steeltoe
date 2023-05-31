// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.EntityFrameworkCore.SqlServer.DynamicTypeAccess;

/// <summary>
/// Provides access to types in Entity Framework Core NuGet packages for SQL Server, without referencing them.
/// </summary>
internal sealed class SqlServerEntityFrameworkCorePackageResolver : PackageResolver
{
    public TypeAccessor SqlServerDbContextOptionsExtensionsClass => ResolveType("Microsoft.EntityFrameworkCore.SqlServerDbContextOptionsExtensions");
    public TypeAccessor SqlServerDbContextOptionsBuilderClass => ResolveType("Microsoft.EntityFrameworkCore.Infrastructure.SqlServerDbContextOptionsBuilder");
    public TypeAccessor SqlConnectionClass => ResolveType("Microsoft.Data.SqlClient.SqlConnection");

    public SqlServerEntityFrameworkCorePackageResolver()
        : base(new[]
        {
            "Microsoft.EntityFrameworkCore.SqlServer",
            "Microsoft.Data.SqlClient"
        }, new[]
        {
            "Microsoft.EntityFrameworkCore.SqlServer"
        })
    {
    }
}
