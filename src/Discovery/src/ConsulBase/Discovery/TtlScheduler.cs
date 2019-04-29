// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
        internal readonly ConcurrentDictionary<string, Timer> _serviceHeartbeats = new ConcurrentDictionary<string, Timer>(StringComparer.OrdinalIgnoreCase);

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

                var timer = new Timer(async s => { await PassTtl(s.ToString()); }, checkId, TimeSpan.Zero, interval);
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

        /// <summary>
        /// Remove all heart beats from scheduler
        /// </summary>
        public void Dispose()
        {
            foreach (var instance in _serviceHeartbeats.Keys)
            {
                Remove(instance);
            }
        }

        private Task PassTtl(string serviceId)
        {
            _logger?.LogDebug("Sending consul heartbeat for: {serviceId} ", serviceId);

            try
            {
                return _client.Agent.PassTTL(serviceId, "ttl");
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Exception sending consul heartbeat for: {serviceId} ", serviceId);
            }

            return Task.CompletedTask;
        }
    }
}