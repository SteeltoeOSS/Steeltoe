// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.CosmosDb.DynamicTypeAccess;

/// <summary>
/// Provides access to types in CosmosDB NuGet packages, without referencing them.
/// </summary>
internal sealed class CosmosDbPackageResolver : PackageResolver
{
    public static readonly CosmosDbPackageResolver Default = new("Microsoft.Azure.Cosmos.Client", "Microsoft.Azure.Cosmos");

    public TypeAccessor CosmosClientClass => ResolveType("Microsoft.Azure.Cosmos.CosmosClient");

    private CosmosDbPackageResolver(string assemblyName, string packageName)
        : base(assemblyName, packageName)
    {
    }
}
