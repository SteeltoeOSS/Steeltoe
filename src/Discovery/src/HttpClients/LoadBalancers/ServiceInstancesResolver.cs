// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;

namespace Steeltoe.Discovery.HttpClients.LoadBalancers;

/// <summary>
/// Queries all discovery clients for service instances, optionally caching the results using <see cref="IDistributedCache" />.
/// </summary>
public sealed class ServiceInstancesResolver
{
    private readonly IDiscoveryClient[] _discoveryClients;
    private readonly IDistributedCache? _distributedCache;
    private readonly DistributedCacheEntryOptions _cacheEntryOptions;
    private readonly ILogger<ServiceInstancesResolver> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceInstancesResolver" /> class.
    /// </summary>
    /// <param name="discoveryClients">
    /// Used to retrieve the available service instances.
    /// </param>
    /// <param name="logger">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public ServiceInstancesResolver(IEnumerable<IDiscoveryClient> discoveryClients, ILogger<ServiceInstancesResolver> logger)
        : this(discoveryClients, null, null, logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceInstancesResolver" /> class.
    /// </summary>
    /// <param name="discoveryClients">
    /// Used to retrieve the available service instances.
    /// </param>
    /// <param name="distributedCache">
    /// For caching the service instances.
    /// </param>
    /// <param name="cacheEntryOptions">
    /// Configuration for <paramref name="distributedCache" />.
    /// </param>
    /// <param name="logger">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public ServiceInstancesResolver(IEnumerable<IDiscoveryClient> discoveryClients, IDistributedCache? distributedCache,
        DistributedCacheEntryOptions? cacheEntryOptions, ILogger<ServiceInstancesResolver> logger)
    {
        ArgumentNullException.ThrowIfNull(discoveryClients);
        ArgumentNullException.ThrowIfNull(logger);

        IDiscoveryClient[] discoveryClientArray = discoveryClients.ToArray();
        ArgumentGuard.ElementsNotNull(discoveryClientArray);

        _discoveryClients = discoveryClientArray;
        _distributedCache = distributedCache;
        _cacheEntryOptions = cacheEntryOptions ?? new DistributedCacheEntryOptions();
        _logger = logger;

        if (_discoveryClients.Length == 0)
        {
            _logger.LogWarning("No discovery clients are registered.");
        }
    }

    public async Task<IList<IServiceInstance>> ResolveInstancesAsync(string serviceId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);

        string cacheKey = $"Steeltoe:Discovery:ServiceInstances:{serviceId}";

        if (_distributedCache != null)
        {
            byte[]? cacheValue = await _distributedCache.GetAsync(cacheKey, cancellationToken);
            List<IServiceInstance>? instancesFromCache = FromCacheValue(cacheValue);

            if (instancesFromCache != null)
            {
                _logger.LogDebug("Returning {Count} instances from cache.", instancesFromCache.Count);
                return instancesFromCache;
            }
        }

        List<IServiceInstance> instances = [];

        foreach (IDiscoveryClient discoveryClient in _discoveryClients)
        {
            try
            {
                IList<IServiceInstance> instancesPerClient = await discoveryClient.GetInstancesAsync(serviceId, cancellationToken);
                instances.AddRange(instancesPerClient);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to get instances from {DiscoveryClient}.", discoveryClient.GetType());
            }
        }

        if (_distributedCache != null)
        {
            byte[] cacheValue = ToCacheValue(instances);
            await _distributedCache.SetAsync(cacheKey, cacheValue, _cacheEntryOptions, cancellationToken);
        }

        return instances;
    }

    private static List<IServiceInstance>? FromCacheValue(byte[]? cacheValue)
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
        JsonSerializableServiceInstance[] serializableInstances = instances.Select(JsonSerializableServiceInstance.CopyFrom).ToArray();
        return JsonSerializer.SerializeToUtf8Bytes(serializableInstances);
    }

    private sealed class JsonSerializableServiceInstance : IServiceInstance
    {
        // Trust that deserialized instances meet the IServiceInstance contract, so suppress nullability warnings.

        public string ServiceId { get; set; } = null!;
        public string InstanceId { get; set; } = null!;
        public string Host { get; set; } = null!;
        public int Port { get; set; }
        public bool IsSecure { get; set; }
        public Uri Uri { get; set; } = null!;
        public IReadOnlyDictionary<string, string?> Metadata { get; set; } = null!;

        public static JsonSerializableServiceInstance CopyFrom(IServiceInstance instance)
        {
            ArgumentNullException.ThrowIfNull(instance);

            return new JsonSerializableServiceInstance
            {
                ServiceId = instance.ServiceId,
                InstanceId = instance.InstanceId,
                Host = instance.Host,
                Port = instance.Port,
                IsSecure = instance.IsSecure,
                Uri = instance.Uri,
                Metadata = instance.Metadata
            };
        }
    }
}
