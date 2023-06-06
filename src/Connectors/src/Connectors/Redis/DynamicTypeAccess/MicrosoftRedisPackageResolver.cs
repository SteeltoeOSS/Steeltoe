// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.Redis.DynamicTypeAccess;

/// <summary>
/// Provides access to types in the Microsoft.Extensions.Caching.StackExchangeRedis NuGet package, without referencing it.
/// </summary>
internal sealed class MicrosoftRedisPackageResolver : PackageResolver
{
    public static readonly MicrosoftRedisPackageResolver Default = new(new[]
    {
        "Microsoft.Extensions.Caching.Abstractions",
        "Microsoft.Extensions.Caching.StackExchangeRedis"
    }, new[]
    {
        "Microsoft.Extensions.Caching.StackExchangeRedis"
    });

    public TypeAccessor DistributedCacheInterface => ResolveType("Microsoft.Extensions.Caching.Distributed.IDistributedCache");
    public TypeAccessor RedisCacheOptionsClass => ResolveType("Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions");
    public TypeAccessor RedisCacheClass => ResolveType("Microsoft.Extensions.Caching.StackExchangeRedis.RedisCache");

    private MicrosoftRedisPackageResolver(IReadOnlyList<string> assemblyNames, IReadOnlyList<string> packageNames)
        : base(assemblyNames, packageNames)
    {
    }

    protected override bool IsAvailable(IEnumerable<string> assemblyNames)
    {
        return base.IsAvailable(new[]
        {
            "Microsoft.Extensions.Caching.StackExchangeRedis"
        });
    }
}
