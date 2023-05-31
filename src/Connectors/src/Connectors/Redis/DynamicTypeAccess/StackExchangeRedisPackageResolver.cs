// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.Redis.DynamicTypeAccess;

/// <summary>
/// Provides access to types in the StackExchange.Redis NuGet package, without referencing it.
/// </summary>
internal sealed class StackExchangeRedisPackageResolver : PackageResolver
{
    public TypeAccessor ConnectionMultiplexerInterface => ResolveType("StackExchange.Redis.IConnectionMultiplexer");
    public TypeAccessor ConnectionMultiplexerClass => ResolveType("StackExchange.Redis.ConnectionMultiplexer");

    public StackExchangeRedisPackageResolver()
        : base("StackExchange.Redis", "StackExchange.Redis")
    {
    }
}
