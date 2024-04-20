// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;

namespace Steeltoe.Discovery.HttpClients.LoadBalancers;

internal static class CachingServiceInstancesResolver
{
    public static async Task<IList<IServiceInstance>> GetInstancesWithCacheAsync(ICollection<IDiscoveryClient> discoveryClients, string serviceId,
        IDistributedCache? distributedCache, DistributedCacheEntryOptions? cacheEntryOptions, string? serviceInstancesKeyPrefix,
        CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(discoveryClients);
        ArgumentGuard.NotNull(serviceId);

        string cacheKey = $"{serviceInstancesKeyPrefix ?? "Steeltoe-ServiceInstances:"}{serviceId}";

        if (distributedCache != null)
        {
            byte[]? cacheValue = await distributedCache.GetAsync(cacheKey, cancellationToken);
            IList<IServiceInstance>? instancesFromCache = FromCacheValue(cacheValue);

            if (instancesFromCache != null)
            {
                return instancesFromCache;
            }
        }

        List<IServiceInstance> instances = [];

        foreach (IDiscoveryClient discoveryClient in discoveryClients)
        {
            IList<IServiceInstance> instancesPerClient = await discoveryClient.GetInstancesAsync(serviceId, cancellationToken);
            instances.AddRange(instancesPerClient);
        }

        if (distributedCache != null)
        {
            byte[] cacheValue = ToCacheValue(instances);
            await distributedCache.SetAsync(cacheKey, cacheValue, cacheEntryOptions ?? new DistributedCacheEntryOptions(), cancellationToken);
        }

        return instances;
    }

    private static IList<IServiceInstance>? FromCacheValue(byte[]? cacheValue)
    {
        if (cacheValue is { Length: > 0 })
        {
            var serializableInstances = JsonSerializer.Deserialize<List<JsonSerializableServiceInstance>>(cacheValue);

            if (serializableInstances != null)
            {
                return serializableInstances.ToList<IServiceInstance>();
            }
        }

        return null;
    }

    private static byte[] ToCacheValue(IEnumerable<IServiceInstance> instances)
    {
        List<JsonSerializableServiceInstance> serializableInstances = instances.Select(JsonSerializableServiceInstance.CopyFrom).ToList();
        return JsonSerializer.SerializeToUtf8Bytes(serializableInstances);
    }

    private sealed class JsonSerializableServiceInstance : IServiceInstance
    {
        // Trust that deserialized instances meet the IServiceInstance contract, so suppress nullability warnings.

        public string ServiceId { get; set; } = null!;
        public string Host { get; set; } = null!;
        public int Port { get; set; }
        public bool IsSecure { get; set; }
        public Uri Uri { get; set; } = null!;
        public IReadOnlyDictionary<string, string?> Metadata { get; set; } = null!;

        public static JsonSerializableServiceInstance CopyFrom(IServiceInstance instance)
        {
            ArgumentGuard.NotNull(instance);

            return new JsonSerializableServiceInstance
            {
                ServiceId = instance.ServiceId,
                Host = instance.Host,
                Port = instance.Port,
                IsSecure = instance.IsSecure,
                Uri = instance.Uri,
                Metadata = instance.Metadata
            };
        }
    }
}
