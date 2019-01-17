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
using Steeltoe.Common.Discovery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Steeltoe.Discovery.Consul.Discovery
{
    public class ConsulDiscoveryClient : IDiscoveryClient
    {
        private readonly ConsulClient _client;
        private readonly IOptionsMonitor<ConsulDiscoveryOptions> _discoveryOptionsMonitor;

        private ConsulDiscoveryOptions ConsulDiscoveryOptions => _discoveryOptionsMonitor.CurrentValue;

        private IServiceInstance _thisServiceInstance;

        public ConsulDiscoveryClient(ConsulClient client, IOptionsMonitor<ConsulDiscoveryOptions> discoveryOptionsMonitor)
        {
            _client = client;
            _discoveryOptionsMonitor = discoveryOptionsMonitor;

            _thisServiceInstance = new ThisServiceInstance(discoveryOptionsMonitor.CurrentValue);
            discoveryOptionsMonitor.OnChange(o => { _thisServiceInstance = new ThisServiceInstance(discoveryOptionsMonitor.CurrentValue); });
        }

        #region Implementation of IDiscoveryClient

        /// <inheritdoc/>
        public IServiceInstance GetLocalServiceInstance()
        {
            return _thisServiceInstance;
        }

        /// <inheritdoc/>
        public IList<IServiceInstance> GetInstances(string serviceId)
        {
            var instances = new List<IServiceInstance>();
            AddInstancesToListAsync(instances, serviceId).GetAwaiter().GetResult();
            return instances;
        }

        /// <inheritdoc/>
        public Task ShutdownAsync()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public string Description { get; } = "HashiCorp Consul Client";

        /// <inheritdoc/>
        public IList<string> Services => GetServicesAsync().GetAwaiter().GetResult();

        #endregion Implementation of IDiscoveryClient

        private static ConsulClient CreateConsulClient(ConsulOptions options)
        {
            return new ConsulClient(s =>
            {
                s.Address = new Uri($"{options.Scheme}://{options.Host}:{options.Port}");
            });
        }

        private async Task AddInstancesToListAsync(ICollection<IServiceInstance> instances, string serviceId)
        {
            var result = await _client.Health.Service(serviceId, ConsulDiscoveryOptions.DefaultQueryTag, ConsulDiscoveryOptions.QueryPassing);
            var response = result.Response;

            foreach (var instance in response.Select(s => new ConsulServiceInstance(s)))
            {
                instances.Add(instance);
            }
        }

        private async Task<IList<string>> GetServicesAsync()
        {
            var result = await _client.Catalog.Services();
            var response = result.Response;
            return response.Keys.ToList();
        }
    }
}