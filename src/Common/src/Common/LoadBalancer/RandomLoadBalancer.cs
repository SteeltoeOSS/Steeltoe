// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Discovery;
using System;
using System.Threading.Tasks;

namespace Steeltoe.Common.LoadBalancer;

public class RandomLoadBalancer : ILoadBalancer
{
    private static readonly Random _random = new ();
    private readonly IServiceInstanceProvider _serviceInstanceProvider;
    private readonly IDistributedCache _distributedCache;
    private readonly DistributedCacheEntryOptions _cacheOptions;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomLoadBalancer"/> class.
    /// Returns random service instances, with option caching of service lookups
    /// </summary>
    /// <param name="serviceInstanceProvider">Provider of service instance information</param>
    /// <param name="distributedCache">For caching service instance data</param>
    /// <param name="cacheEntryOptions">Configuration for cache entries of service instance data</param>
    /// <param name="logger">For logging</param>
    public RandomLoadBalancer(IServiceInstanceProvider serviceInstanceProvider, IDistributedCache distributedCache = null, DistributedCacheEntryOptions cacheEntryOptions = null, ILogger logger = null)
    {
        _serviceInstanceProvider = serviceInstanceProvider ?? throw new ArgumentNullException(nameof(serviceInstanceProvider));
        _distributedCache = distributedCache;
        _cacheOptions = cacheEntryOptions;
        _logger = logger;
    }

    public virtual async Task<Uri> ResolveServiceInstanceAsync(Uri request)
    {
        _logger?.LogTrace("ResolveServiceInstance {serviceInstance}", request.Host);
        var availableServiceInstances = await _serviceInstanceProvider.GetInstancesWithCacheAsync(request.Host, _distributedCache, _cacheOptions).ConfigureAwait(false);
        if (availableServiceInstances.Count > 0)
        {
            // load balancer instance selection predictability is not likely to be a security concern
            var resolvedUri = availableServiceInstances[_random.Next(availableServiceInstances.Count)].Uri;
            _logger?.LogDebug("Resolved {url} to {service}", request.Host, resolvedUri.Host);
            return new Uri(resolvedUri, request.PathAndQuery);
        }
        else
        {
            _logger?.LogWarning("Attempted to resolve service for {url} but found 0 instances", request.Host);
            return request;
        }
    }

    public virtual Task UpdateStatsAsync(Uri originalUri, Uri resolvedUri, TimeSpan responseTime, Exception exception)
    {
        return Task.CompletedTask;
    }
}