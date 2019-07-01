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
#pragma warning disable S1144 // Unused private types or members should be removed
        private const string UNKNOWN = "UNKNOWN";
        private const string UP = "UP";
        private const string DOWN = "DOWN";
        private const string OUT_OF_SERVICE = "OUT_OF_SERVICE";
#pragma warning restore S1144 // Unused private types or members should be removed

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
        public Task RegisterAsync(IConsulRegistration registration)
        {
            if (registration == null)
            {
                throw new ArgumentNullException(nameof(registration));
            }

            return RegisterAsyncInternal(registration);
        }

        private async Task RegisterAsyncInternal(IConsulRegistration registration)
        {
            _logger?.LogInformation("Registering service with consul {serviceId} ", registration.ServiceId);

            try
            {
                await _client.Agent.ServiceRegister(registration.Service).ConfigureAwait(false);
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

#pragma warning disable SA1202 // Elements must be ordered by access
                              /// <inheritdoc/>
        public Task DeregisterAsync(IConsulRegistration registration)
        {
            if (registration == null)
            {
                throw new ArgumentNullException(nameof(registration));
            }

            return DeregisterAsyncInternal(registration);
        }

        private async Task DeregisterAsyncInternal(IConsulRegistration registration)
        {
            if (Options.IsHeartBeatEnabled && _scheduler != null)
            {
                _scheduler.Remove(registration.InstanceId);
            }

            _logger?.LogInformation("Deregistering service with consul {instanceId} ", registration.InstanceId);

            await _client.Agent.ServiceDeregister(registration.InstanceId).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task SetStatusAsync(IConsulRegistration registration, string status)
        {
            if (registration == null)
            {
                throw new ArgumentNullException(nameof(registration));
            }

            return SetStatusAsyncInternal(registration, status);
        }

        private async Task SetStatusAsyncInternal(IConsulRegistration registration, string status)
        {
            if (OUT_OF_SERVICE.Equals(status, StringComparison.OrdinalIgnoreCase))
            {
                await _client.Agent.EnableServiceMaintenance(registration.InstanceId, OUT_OF_SERVICE).ConfigureAwait(false);
            }
            else if (UP.Equals(status, StringComparison.OrdinalIgnoreCase))
            {
                await _client.Agent.DisableServiceMaintenance(registration.InstanceId).ConfigureAwait(false);
            }
            else
            {
                throw new ArgumentException($"Unknown status: {status}");
            }
        }

        /// <inheritdoc/>
        public Task<object> GetStatusAsync(IConsulRegistration registration)
        {
            if (registration == null)
            {
                throw new ArgumentNullException(nameof(registration));
            }

            return GetStatusAsyncInternal(registration);
        }

        public async Task<object> GetStatusAsyncInternal(IConsulRegistration registration)
        {
            var response = await _client.Health.Checks(registration.ServiceId, QueryOptions.Default).ConfigureAwait(false);
            var checks = response.Response;

            foreach (HealthCheck check in checks)
            {
                if (check.ServiceID.Equals(registration.InstanceId) && check.Name.Equals("Service Maintenance Mode", StringComparison.OrdinalIgnoreCase))
                {
                    return OUT_OF_SERVICE;
                }
            }

            return UP;
        }
#pragma warning restore SA1202 // Elements must be ordered by access

        /// <inheritdoc/>
        public void Register(IConsulRegistration registration)
        {
            RegisterAsync(registration).GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public void Deregister(IConsulRegistration registration)
        {
            DeregisterAsync(registration).GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public void SetStatus(IConsulRegistration registration, string status)
        {
            SetStatusAsync(registration, status).GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public S GetStatus<S>(IConsulRegistration registration)
            where S : class
        {
            var result = GetStatusAsync(registration).GetAwaiter().GetResult();

            return (S)result;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _scheduler?.Dispose();
        }
    }
}