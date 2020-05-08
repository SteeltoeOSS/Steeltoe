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
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Consul.Registry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Steeltoe.Discovery.Consul.Discovery
{
    /// <summary>
    /// A IDiscoveryClient implementation for Consul
    /// </summary>
    public class ConsulDiscoveryClient : IConsulDiscoveryClient
    {
        private readonly IConsulClient _client;
        private readonly ILogger<ConsulDiscoveryClient> _logger;
        private readonly IOptionsMonitor<ConsulDiscoveryOptions> _optionsMonitor;
        private readonly ConsulDiscoveryOptions _options;
        private readonly IServiceInstance _thisServiceInstance;
        private readonly IConsulServiceRegistrar _registrar;

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
        /// Initializes a new instance of the <see cref="ConsulDiscoveryClient"/> class.
        /// </summary>
        /// <param name="client">a Consul client</param>
        /// <param name="options">the configuration options</param>
        /// <param name="registrar">a Consul registrar service</param>
        /// <param name="logger">optional logger</param>
        public ConsulDiscoveryClient(IConsulClient client, ConsulDiscoveryOptions options, IConsulServiceRegistrar registrar = null, ILogger<ConsulDiscoveryClient> logger = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
            _registrar = registrar;

            if (_registrar != null)
            {
                _registrar.Start();
                _thisServiceInstance = new ThisServiceInstance(_registrar.Registration);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsulDiscoveryClient"/> class.
        /// </summary>
        /// <param name="client">a Consule client</param>
        /// <param name="optionsMonitor">the configuration options</param>
        /// <param name="registrar">a Consul registrar service</param>
        /// <param name="logger">optional logger</param>
        public ConsulDiscoveryClient(IConsulClient client, IOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor, IConsulServiceRegistrar registrar = null, ILogger<ConsulDiscoveryClient> logger = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
            _logger = logger;
            _registrar = registrar;

            if (_registrar != null)
            {
                _registrar.Start();
                _thisServiceInstance = new ThisServiceInstance(_registrar.Registration);
            }
        }

        #region Implementation of IDiscoveryClient

        /// <inheritdoc/>
        public IServiceInstance GetLocalServiceInstance()
        {
            return _thisServiceInstance;
        }

#pragma warning disable S4136 // Method overloads should be grouped together
        /// <inheritdoc/>
        public IList<IServiceInstance> GetInstances(string serviceId)
#pragma warning restore S4136 // Method overloads should be grouped together
        {
            return GetInstances(serviceId, QueryOptions.Default);
        }

        /// <inheritdoc/>
        public Task ShutdownAsync()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public string Description { get; } = "HashiCorp Consul Client";

        /// <inheritdoc/>
        public IList<string> Services
        {
            get
            {
                return GetServicesAsync().GetAwaiter().GetResult();
            }
        }

        #endregion Implementation of IDiscoveryClient

        /// <summary>
        /// Returns the instances for the provided service id
        /// </summary>
        /// <param name="serviceId">the service id to get instances for</param>
        /// <param name="queryOptions">any Consul query options to use when doing lookup</param>
        /// <returns>the list of service instances</returns>
        public IList<IServiceInstance> GetInstances(string serviceId, QueryOptions queryOptions = null)
        {
            return GetInstancesAsync(serviceId, queryOptions).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Returns all instances for all services
        /// </summary>
        /// <param name="queryOptions">any Consul query options to use when doing lookup</param>
        /// <returns>the list of service instances</returns>
        public IList<IServiceInstance> GetAllInstances(QueryOptions queryOptions = null)
        {
            return GetAllInstancesAsync().GetAwaiter().GetResult();

            async Task<IList<IServiceInstance>> GetAllInstancesAsync()
            {
                queryOptions ??= QueryOptions.Default;
                var instances = new List<IServiceInstance>();
                var result = await GetServicesAsync().ConfigureAwait(false);
                foreach (var serviceId in result)
                {
                    await AddInstancesToListAsync(instances, serviceId, queryOptions).ConfigureAwait(false);
                }

                return instances;
            }
        }

        /// <summary>
        /// Returns a list of service names in the catalog
        /// </summary>
        /// <param name="queryOptions">any Consul query options to use when doing lookup</param>
        /// <returns>the list of services</returns>
        public IList<string> GetServices(QueryOptions queryOptions = null)
        {
            queryOptions ??= QueryOptions.Default;
            return GetServicesAsync(queryOptions).GetAwaiter().GetResult();
        }

        internal async Task<IList<IServiceInstance>> GetInstancesAsync(string serviceId, QueryOptions queryOptions)
        {
            var instances = new List<IServiceInstance>();
            await AddInstancesToListAsync(instances, serviceId, queryOptions).ConfigureAwait(false);
            return instances;
        }

        internal async Task<IList<string>> GetServicesAsync(QueryOptions queryOptions = null)
        {
            queryOptions ??= QueryOptions.Default;
            var result = await _client.Catalog.Services(queryOptions).ConfigureAwait(false);
            var response = result.Response;
            return response.Keys.ToList();
        }

        internal async Task AddInstancesToListAsync(ICollection<IServiceInstance> instances, string serviceId, QueryOptions queryOptions)
        {
            var result = await _client.Health.Service(serviceId, Options.DefaultQueryTag, Options.QueryPassing, queryOptions).ConfigureAwait(false);
            var response = result.Response;

            foreach (var instance in response.Select(s => new ConsulServiceInstance(s)))
            {
                instances.Add(instance);
            }
        }

        private bool disposed = false;

        /// <summary>
        /// Dispose of the client and also the Consul service registrar if provided
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing && _registrar != null)
                {
                    _registrar.Dispose();
                }

                disposed = true;
            }
        }

        ~ConsulDiscoveryClient()
        {
            Dispose(false);
        }
    }
}