// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Steeltoe.Common.Discovery;

public static class ServiceInstanceProviderExtensions
{
    public static async Task<IList<IServiceInstance>> GetInstancesWithCacheAsync(this IServiceInstanceProvider serviceInstanceProvider, string serviceId,
        IDistributedCache distributedCache = null, DistributedCacheEntryOptions cacheOptions = null, string serviceInstancesKeyPrefix = "ServiceInstances:")
    {
        // if distributed cache was provided, just make the call back to the provider
        if (distributedCache != null)
        {
            // check the cache for existing service instances
            byte[] instanceData = await distributedCache.GetAsync(serviceInstancesKeyPrefix + serviceId).ConfigureAwait(false);

            if (instanceData != null && instanceData.Length > 0)
            {
                return DeserializeFromCache<List<SerializableIServiceInstance>>(instanceData).ToList<IServiceInstance>();
            }
        }

        // cache not found or instances not found, call out to the provider
        IList<IServiceInstance> instances = serviceInstanceProvider.GetInstances(serviceId);

        if (distributedCache != null)
        {
            await distributedCache.SetAsync(serviceInstancesKeyPrefix + serviceId, SerializeForCache(MapToSerializable(instances)),
                cacheOptions ?? new DistributedCacheEntryOptions()).ConfigureAwait(false);
        }

        return instances;
    }

    private static List<SerializableIServiceInstance> MapToSerializable(IList<IServiceInstance> instances)
    {
        IEnumerable<SerializableIServiceInstance> inst = instances.Select(i => new SerializableIServiceInstance(i));
        return inst.ToList();
    }

    private static byte[] SerializeForCache(object data)
    {
        return JsonSerializer.SerializeToUtf8Bytes(data);
    }

    private static T DeserializeFromCache<T>(byte[] data)
        where T : class
    {
        return JsonSerializer.Deserialize<T>(data);
    }
}
