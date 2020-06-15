// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Caching.Distributed;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace Steeltoe.Common.Discovery
{
    public static class IServiceInstanceProviderExtensions
    {
        public static async Task<IList<IServiceInstance>> GetInstancesWithCacheAsync(this IServiceInstanceProvider serviceInstanceProvider, string serviceId, IDistributedCache distributedCache = null, string serviceInstancesKeyPrefix = "ServiceInstances-")
        {
            // if distributed cache was provided, just make the call back to the provider
            if (distributedCache != null)
            {
                // check the cache for existing service instances
                var instanceData = await distributedCache.GetAsync(serviceInstancesKeyPrefix + serviceId).ConfigureAwait(false);
                if (instanceData != null && instanceData.Length > 0)
                {
                    return DeserializeFromCache<List<SerializableIServiceInstance>>(instanceData).ToList<IServiceInstance>();
                }
            }

            // cache not found or instances not found, call out to the provider
            var instances = serviceInstanceProvider.GetInstances(serviceId);
            if (distributedCache != null)
            {
                await distributedCache.SetAsync(serviceInstancesKeyPrefix + serviceId, SerializeForCache(MapToSerializable(instances))).ConfigureAwait(false);
            }

            return instances;
        }

        private static List<SerializableIServiceInstance> MapToSerializable(IList<IServiceInstance> instances)
        {
            var inst = instances.Select(i => new SerializableIServiceInstance(i));
            return inst.ToList();
        }

        private static byte[] SerializeForCache(object data)
        {
            using (var stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, data);
                return stream.ToArray();
            }
        }

        private static T DeserializeFromCache<T>(byte[] data)
            where T : class
        {
            using (var stream = new MemoryStream(data))
            {
                return new BinaryFormatter().Deserialize(stream) as T;
            }
        }
    }
}
