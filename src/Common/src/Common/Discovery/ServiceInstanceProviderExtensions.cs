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
        string cacheKey = $"{serviceInstancesKeyPrefix}{serviceId}";

        if (distributedCache != null)
        {
            byte[] cacheValue = await distributedCache.GetAsync(cacheKey).ConfigureAwait(false);
            IList<IServiceInstance> instancesFromCache = FromCacheValue(cacheValue);

            if (instancesFromCache != null)
            {
                return instancesFromCache;
            }
        }

        IList<IServiceInstance> instances = serviceInstanceProvider.GetInstances(serviceId);

        if (distributedCache != null && instances != null)
        {
            byte[] cacheValue = ToCacheValue(instances);
            await distributedCache.SetAsync(cacheKey, cacheValue, cacheOptions ?? new DistributedCacheEntryOptions()).ConfigureAwait(false);
        }

        return instances;
    }

    private static IList<IServiceInstance> FromCacheValue(byte[] cacheValue)
    {
        if (cacheValue != null && cacheValue.Length > 0)
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
        List<JsonSerializableServiceInstance> serializableInstances = instances.Select(instance => new JsonSerializableServiceInstance(instance)).ToList();
        return JsonSerializer.SerializeToUtf8Bytes(serializableInstances);
    }
}
