// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.CosmosDb.DynamicTypeAccess;

/// <summary>
/// Provides access to types in CosmosDB NuGet packages, without referencing them.
/// </summary>
internal sealed class CosmosDbPackageResolver : PackageResolver
{
    public TypeAccessor CosmosClientClass => ResolveType("Microsoft.Azure.Cosmos.CosmosClient");

    public CosmosDbPackageResolver()
        : base("Microsoft.Azure.Cosmos.Client", "Microsoft.Azure.Cosmos")
    {
    }
}
