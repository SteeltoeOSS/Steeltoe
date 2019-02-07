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

namespace Steeltoe.Discovery.Consul.Registry
{
    /// <summary>
    /// An implementation of a Consul service registry
    /// </summary>
    public class ConsulServiceRegistry : IConsulServiceRegistry
    {
        private const string UNKNOWN = "UNKNOWN";
        private const string UP = "UP";
        private const string DOWN = "DOWN";
        private const string OUT_OF_SERVICE = "OUT_OF_SERVICE";

        private readonly IConsulClient _client;
        private readonly IScheduler _scheduler;
        private readonly ILogger<ConsulServiceRegistry> _logger;

        private readonly IOptionsMonitor<ConsulDiscoveryOptions> _optionsMonitor;
        private readonly ConsulDiscoveryOptions _options;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsulServiceRegistry"/> class.
        /// </summary>
        /// <param name="client">the Consul client to use</param>
        /// <param name="options">the configuration options</param>
        /// <param name="scheduler">a scheduler to use for heart beats</param>
        /// <param name="logger">an optional logger</param>
        public ConsulServiceRegistry(IConsulClient client, ConsulDiscoveryOptions options, IScheduler scheduler = null, ILogger<ConsulServiceRegistry> logger = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _scheduler = scheduler;
            _logger = logger;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsulServiceRegistry"/> class.
        /// </summary>
        /// <param name="client">the Consul client to use</param>
        /// <param name="optionsMonitor">the configuration options</param>
        /// <param name="scheduler">a scheduler to use for heart beats</param>
        /// <param name="logger">an optional logger</param>
        public ConsulServiceRegistry(IConsulClient client, IOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor, IScheduler scheduler = null, ILogger<ConsulServiceRegistry> logger = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
            _scheduler = scheduler;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task RegisterAsync(IConsulRegistration registration)
        {
            if (registration == null)
            {
                throw new ArgumentNullException(nameof(registration));
            }

            _logger?.LogInformation("Registering service with consul {serviceId} ", registration.ServiceId);

            try
            {
                await _client.Agent.ServiceRegister(registration.Service);
                if (Options.IsHeartBeatEnabled && _scheduler != null)
                {
                    _scheduler.Add(registration.InstanceId);
                }
            }
            catch (Exception e)
            {
                if (Options.FailFast)
                {
                    _logger?.LogError(e, "Error registering service with consul {serviceId} ", registration.ServiceId);
                    throw;
                }

                _logger?.LogWarning(e, "Failfast is false. Error registering service with consul {serviceId} ", registration.ServiceId);
            }
        }

        /// <inheritdoc/>
        public async Task DeregisterAsync(IConsulRegistration registration)
        {
            if (registration == null)
            {
                throw new ArgumentNullException(nameof(registration));
            }

            if (Options.IsHeartBeatEnabled && _scheduler != null)
            {
                _scheduler.Remove(registration.InstanceId);
            }

            _logger?.LogInformation("Deregistering service with consul {instanceId} ", registration.InstanceId);

            await _client.Agent.ServiceDeregister(registration.InstanceId);
        }

        /// <inheritdoc/>
        public async Task SetStatusAsync(IConsulRegistration registration, string status)
        {
            if (registration == null)
            {
                throw new ArgumentNullException(nameof(registration));
            }

            if (OUT_OF_SERVICE.Equals(status, StringComparison.OrdinalIgnoreCase))
            {
                await _client.Agent.EnableServiceMaintenance(registration.InstanceId, OUT_OF_SERVICE);
            }
            else if (UP.Equals(status, StringComparison.OrdinalIgnoreCase))
            {
                await _client.Agent.DisableServiceMaintenance(registration.InstanceId);
            }
            else
            {
                throw new ArgumentException($"Unknown status: {status}");
            }
        }

        /// <inheritdoc/>
        public async Task<object> GetStatusAsync(IConsulRegistration registration)
        {
            if (registration == null)
            {
                throw new ArgumentNullException(nameof(registration));
            }

            var response = await _client.Health.Checks(registration.ServiceId, QueryOptions.Default);
            var checks = response.Response;

            foreach (HealthCheck check in checks)
            {
                if (check.ServiceID.Equals(registration.InstanceId))
                {
                    if (check.Name.Equals("Service Maintenance Mode", StringComparison.OrdinalIgnoreCase))
                    {
                        return OUT_OF_SERVICE;
                    }
                }
            }

            return UP;
        }

        /// <inheritdoc/>
        public void Register(IConsulRegistration registration)
        {
            Task.Run(async () =>
            {
                await RegisterAsync(registration);
            }).Wait();
        }

        /// <inheritdoc/>
        public void Deregister(IConsulRegistration registration)
        {
            Task.Run(async () =>
            {
                await DeregisterAsync(registration);
            }).Wait();
        }

        /// <inheritdoc/>
        public void SetStatus(IConsulRegistration registration, string status)
        {
            Task.Run(async () =>
            {
                await SetStatusAsync(registration, status);
            }).Wait();
        }

        /// <inheritdoc/>
        public S GetStatus<S>(IConsulRegistration registration)
            where S : class
        {
            var result = Task.Run(async () =>
            {
                return await GetStatusAsync(registration);
            }).Result;

            return (S)result;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _scheduler?.Dispose();
        }
    }
}