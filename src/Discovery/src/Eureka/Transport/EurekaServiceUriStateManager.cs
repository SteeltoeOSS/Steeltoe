// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Discovery.Eureka.Transport;

/// <summary>
/// Keeps track of working and broken Eureka service URIs that are configured.
/// </summary>
public sealed class EurekaServiceUriStateManager
{
    private readonly IOptionsMonitor<EurekaClientOptions> _optionsMonitor;
    private readonly ILogger<EurekaServiceUriStateManager> _logger;

    private readonly object _lockObject = new();
    private readonly ISet<Uri> _failedServiceUris = new HashSet<Uri>();
    private Uri? _lastWorkingServiceUri;

    public EurekaServiceUriStateManager(IOptionsMonitor<EurekaClientOptions> optionsMonitor, ILogger<EurekaServiceUriStateManager> logger)
    {
        ArgumentGuard.NotNull(optionsMonitor);
        ArgumentGuard.NotNull(logger);

        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    /// <summary>
    /// Returns a snapshot of the currently configured Eureka servers, excluding those that failed earlier.
    /// </summary>
    /// <returns>
    /// The Eureka servers, with the last-known working server at the top, if available.
    /// </returns>
    public ServiceUrisSnapshot GetSnapshot()
    {
        return new ServiceUrisSnapshot(this);
    }

    private IList<Uri> GetAvailableServiceUris()
    {
        ISet<Uri> availableServiceUris = GetConfiguredServiceUris();

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

        return availableServiceUris.ToList();
    }

    private ISet<Uri> GetConfiguredServiceUris()
    {
        string serviceUrls = _optionsMonitor.CurrentValue.EurekaServerServiceUrls ?? string.Empty;
        return serviceUrls.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(url => new Uri(url)).ToHashSet();
    }

    /// <summary>
    /// Marks the specified server as working, so it will be tried first in subsequent calls.
    /// </summary>
    /// <param name="serviceUri">
    /// The URI of the Eureka server.
    /// </param>
    public void MarkWorkingServiceUri(Uri serviceUri)
    {
        ArgumentGuard.NotNull(serviceUri);

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
    public void MarkFailingServiceUri(Uri serviceUri)
    {
        ArgumentGuard.NotNull(serviceUri);

        lock (_lockObject)
        {
            _failedServiceUris.Add(serviceUri);
        }
    }

    /// <summary>
    /// Provides a method to sequentially try all available Eureka servers.
    /// </summary>
    public sealed class ServiceUrisSnapshot
    {
        private readonly IList<Uri> _availableServiceUrisSnapshot;
        private int _nextIndex;

        internal ServiceUrisSnapshot(EurekaServiceUriStateManager owner)
        {
            _availableServiceUrisSnapshot = owner.GetAvailableServiceUris();
        }

        public Uri GetNextServiceUri()
        {
            if (_nextIndex >= _availableServiceUrisSnapshot.Count)
            {
                throw new EurekaTransportException("Failed to execute request on all known Eureka servers.");
            }

            return _availableServiceUrisSnapshot[_nextIndex++];
        }
    }
}
