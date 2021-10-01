// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Discovery.Consul.Discovery
{
    /// <summary>
    /// The default scheduler used to issue TTL requests to the Consul server
    /// </summary>
    public class TtlScheduler : IScheduler
    {
        internal readonly ConcurrentDictionary<string, Timer> _serviceHeartbeats = new(StringComparer.OrdinalIgnoreCase);

        internal readonly IConsulClient _client;

        private readonly IOptionsMonitor<ConsulDiscoveryOptions> _optionsMonitor;
        private readonly ConsulDiscoveryOptions _options;
        private readonly ILogger<TtlScheduler> _logger;

        internal ConsulDiscoveryOptions Options
        {
            get
            {
                if (_optionsMonitor != null)
                {
                    return _optionsMonitor.CurrentValue;
                }

                return _options;
            }
        }

        internal ConsulHeartbeatOptions HeartbeatOptions
        {
            get
            {
                return Options.Heartbeat;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TtlScheduler"/> class.
        /// </summary>
        /// <param name="optionsMonitor">configuration options</param>
        /// <param name="client">the Consul client</param>
        /// <param name="logger">optional logger</param>
        public TtlScheduler(IOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor, IConsulClient client, ILogger<TtlScheduler> logger = null)
        {
            _optionsMonitor = optionsMonitor;
            _client = client;
            _logger = logger;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TtlScheduler"/> class.
        /// </summary>
        /// <param name="options">configuration options</param>
        /// <param name="client">the Consul client</param>
        /// <param name="logger">optional logger</param>
        public TtlScheduler(ConsulDiscoveryOptions options, IConsulClient client, ILogger<TtlScheduler> logger = null)
        {
            _options = options;
            _client = client;
            _logger = logger;
        }

        /// <inheritdoc/>
        public void Add(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                throw new ArgumentException(nameof(instanceId));
            }

            _logger?.LogDebug("Add {instanceId}", instanceId);

            if (HeartbeatOptions != null)
            {
                var interval = HeartbeatOptions.ComputeHearbeatInterval();

                var checkId = instanceId;
                if (!checkId.StartsWith("service:"))
                {
                    checkId = "service:" + checkId;
                }

                var timer = new Timer(async s => { await PassTtl(s.ToString()).ConfigureAwait(false); }, checkId, TimeSpan.Zero, interval);
                _serviceHeartbeats.AddOrUpdate(instanceId, timer, (key, oldTimer) =>
                {
                    oldTimer.Dispose();
                    return timer;
                });
            }
        }

        /// <inheritdoc/>
        public void Remove(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                throw new ArgumentException(nameof(instanceId));
            }

            _logger?.LogDebug("Remove {instanceId}", instanceId);

            if (_serviceHeartbeats.TryRemove(instanceId, out var timer))
            {
                timer.Dispose();
            }
        }

        private bool _disposed = false;

        /// <summary>
        /// Remove all heart beats from scheduler
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Cleanup
                    foreach (var instance in _serviceHeartbeats.Keys)
                    {
                        Remove(instance);
                    }
                }

                _disposed = true;
            }
        }

        ~TtlScheduler()
        {
            Dispose(false);
        }

        private async Task PassTtl(string serviceId)
        {
            _logger?.LogDebug("Sending consul heartbeat for: {serviceId} ", serviceId);

            try
            {
                await _client.Agent.PassTTL(serviceId, "ttl");
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Exception sending consul heartbeat for: {serviceId} ", serviceId);
            }
        }
    }
}