// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Discovery.Eureka.Configuration;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka;

/// <summary>
/// Keeps track of working and broken Eureka service URIs that are configured, with stickiness to the last working server.
/// </summary>
public sealed class EurekaServiceUriStateManager
{
    private readonly IOptionsMonitor<EurekaClientOptions> _optionsMonitor;
    private readonly ILogger<EurekaServiceUriStateManager> _logger;

    private readonly object _lockObject = new();
    private readonly HashSet<Uri> _failedServiceUris = [];
    private Uri? _lastWorkingServiceUri;

    public EurekaServiceUriStateManager(IOptionsMonitor<EurekaClientOptions> optionsMonitor, ILogger<EurekaServiceUriStateManager> logger)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(logger);

        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    /// <summary>
    /// Returns a snapshot of the currently configured Eureka servers, excluding those that failed earlier.
    /// </summary>
    /// <returns>
    /// The Eureka servers, with the last-known working server at the top, if available.
    /// </returns>
    internal ServiceUrisSnapshot GetSnapshot()
    {
        return new ServiceUrisSnapshot(this);
    }

    private Uri[] GetAvailableServiceUris()
    {
        HashSet<Uri> availableServiceUris = GetConfiguredServiceUris();

        lock (_lockObject)
        {
            // Purge earlier failing service URIs that are no longer in use.
            _failedServiceUris.IntersectWith(availableServiceUris);

            // If enough hosts are bad, we have no choice but to start over again.
            int threshold = (int)Math.Round(availableServiceUris.Count * 0.67);

            if (_failedServiceUris.Count > 0 && _failedServiceUris.Count >= threshold)
            {
                _logger.LogDebug("Clearing quarantined list of size {Count}.", _failedServiceUris.Count);
                _failedServiceUris.Clear();
            }

            availableServiceUris.ExceptWith(_failedServiceUris);

            // Clear working service URI when it is no longer in use.
            if (_lastWorkingServiceUri != null && !availableServiceUris.Contains(_lastWorkingServiceUri))
            {
                _lastWorkingServiceUri = null;
            }

            // Move working service URI to the top, so it gets tried first.
            if (_lastWorkingServiceUri != null)
            {
                availableServiceUris.ExceptWith([_lastWorkingServiceUri]);

                return
                [
                    _lastWorkingServiceUri,
                    ..availableServiceUris
                ];
            }
        }

        return availableServiceUris.ToArray();
    }

    private HashSet<Uri> GetConfiguredServiceUris()
    {
        string serviceUrls = _optionsMonitor.CurrentValue.EurekaServerServiceUrls ?? string.Empty;
        return serviceUrls.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(EnsureTrailingSlash).ToHashSet();
    }

    private static Uri EnsureTrailingSlash(string url)
    {
        return new Uri(url.EndsWith('/') ? url : url + '/');
    }

    /// <summary>
    /// Marks the specified server as working, so it will be tried first in subsequent calls.
    /// </summary>
    /// <param name="serviceUri">
    /// The URI of the Eureka server.
    /// </param>
    internal void MarkWorkingServiceUri(Uri serviceUri)
    {
        ArgumentNullException.ThrowIfNull(serviceUri);

        lock (_lockObject)
        {
            _lastWorkingServiceUri = serviceUri;
            _failedServiceUris.Remove(serviceUri);
        }
    }

    /// <summary>
    /// Marks the specified server as failing, so it will not be tried anymore in subsequent calls.
    /// </summary>
    /// <param name="serviceUri">
    /// The URI of the Eureka server.
    /// </param>
    internal void MarkFailingServiceUri(Uri serviceUri)
    {
        ArgumentNullException.ThrowIfNull(serviceUri);

        lock (_lockObject)
        {
            _failedServiceUris.Add(serviceUri);
        }
    }

    /// <summary>
    /// Provides a method to sequentially try all available Eureka servers.
    /// </summary>
    internal sealed class ServiceUrisSnapshot
    {
        private readonly Uri[] _availableUris;
        private int _nextIndex;

        internal ServiceUrisSnapshot(EurekaServiceUriStateManager owner)
        {
            _availableUris = owner.GetAvailableServiceUris();
        }

        public Uri GetNextServiceUri()
        {
            if (_nextIndex >= _availableUris.Length)
            {
                throw new EurekaTransportException("Failed to execute request on all known Eureka servers.");
            }

            return _availableUris[_nextIndex++];
        }
    }
}
