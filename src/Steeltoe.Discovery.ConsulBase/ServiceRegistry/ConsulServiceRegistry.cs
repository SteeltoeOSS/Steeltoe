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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Discovery.Consul.Discovery;
using System;
using System.Threading.Tasks;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Steeltoe.Discovery.Consul.ServiceRegistry
{
    public class ConsulServiceRegistry
    {
        private readonly ConsulClient _client;
        private readonly TtlScheduler _ttlScheduler;
        private readonly ILogger<ConsulServiceRegistry> _logger;
        private readonly ConsulDiscoveryOptions _consulDiscoveryOptions;
        private readonly HeartbeatOptions _heartbeatOptions;

        public ConsulServiceRegistry(ConsulClient client, IOptions<ConsulDiscoveryOptions> consulDiscoveryOptionsAccessor, TtlScheduler ttlScheduler, IOptions<HeartbeatOptions> heartbeatOptionsAccessor, ILogger<ConsulServiceRegistry> logger)
        {
            _client = client;
            _consulDiscoveryOptions = consulDiscoveryOptionsAccessor.Value;
            _ttlScheduler = ttlScheduler;
            _heartbeatOptions = heartbeatOptionsAccessor.Value;
            _logger = logger;
        }

        public async Task RegisterAsync(ConsulRegistration registration)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Registering service with consul: " + registration.ServiceId);
            }

            try
            {
                await _client.Agent.ServiceRegister(registration.AgentServiceRegistration);
                if (_heartbeatOptions.Enable)
                {
                    _ttlScheduler?.Add(registration.InstanceId);
                }
            }
            catch (Exception e)
            {
                if (_consulDiscoveryOptions.FailFast)
                {
                    _logger.LogError(e, "Error registering service with consul: " + registration.ServiceId);
                    throw;
                }

                _logger.LogWarning(e, "Failfast is false. Error registering service with consul: " + registration.ServiceId);
            }
        }

        public async Task DeregisterAsync(ConsulRegistration registration)
        {
            _ttlScheduler?.Remove(registration.InstanceId);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Deregistering service with consul: " + registration.ServiceId);
            }

            await _client.Agent.ServiceDeregister(registration.InstanceId);
        }
    }
}