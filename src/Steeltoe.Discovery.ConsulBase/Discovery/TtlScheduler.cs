// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Consul;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Discovery.Consul.Discovery
{
    public class TtlScheduler
    {
        private readonly ConcurrentDictionary<string, Timer> _serviceHeartbeats = new ConcurrentDictionary<string, Timer>(StringComparer.OrdinalIgnoreCase);

        private readonly IOptionsMonitor<HeartbeatOptions> _heartbeatOptionsMonitor;
        private readonly ConsulClient _client;

        private HeartbeatOptions HeartbeatOptions => _heartbeatOptionsMonitor.CurrentValue;

        public TtlScheduler(IOptionsMonitor<HeartbeatOptions> heartbeatOptionsMonitor, ConsulClient client)
        {
            _heartbeatOptionsMonitor = heartbeatOptionsMonitor;
            _client = client;
        }

        public void Add(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                throw new ArgumentNullException(nameof(instanceId));
            }

            var interval = HeartbeatOptions.ComputeHearbeatInterval();
            var timer = new Timer(async s => { await PassTtl(s.ToString()); }, instanceId, TimeSpan.Zero, interval);
            _serviceHeartbeats.AddOrUpdate(instanceId, timer, (key, oldTimer) =>
            {
                oldTimer.Dispose();
                return timer;
            });
        }

        public void Remove(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                throw new ArgumentNullException(nameof(instanceId));
            }

            if (_serviceHeartbeats.TryRemove(instanceId, out var timer))
            {
                timer.Dispose();
            }
        }

        private Task PassTtl(string serviceId)
        {
            if (!serviceId.StartsWith("service:"))
            {
                serviceId = "service:" + serviceId;
            }

            return _client.Agent.PassTTL(serviceId, "ttl");
        }
    }
}