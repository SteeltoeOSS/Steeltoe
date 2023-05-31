// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.MongoDb.DynamicTypeAccess;

/// <summary>
/// Provides access to types in MongoDB NuGet packages, without referencing them.
/// </summary>
internal sealed class MongoDbPackageResolver : PackageResolver
{
    public TypeAccessor MongoClientInterface => ResolveType("MongoDB.Driver.IMongoClient");
    public TypeAccessor MongoClientClass => ResolveType("MongoDB.Driver.MongoClient");

    public MongoDbPackageResolver()
        : base("MongoDB.Driver", "MongoDB.Driver")
    {
    }
}
