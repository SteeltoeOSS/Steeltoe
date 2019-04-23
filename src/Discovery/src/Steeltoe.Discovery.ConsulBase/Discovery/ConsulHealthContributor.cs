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
using Steeltoe.Common.HealthChecks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HealthStatus = Steeltoe.Common.HealthChecks.HealthStatus;

namespace Steeltoe.Discovery.Consul.Discovery
{
    /// <summary>
    /// A Health contributor which provides the health of the Consul server connection
    /// </summary>
    public class ConsulHealthContributor : IHealthContributor
    {
        private readonly IConsulClient _client;
        private readonly ILogger<ConsulHealthContributor> _logger;
        private readonly IOptionsMonitor<ConsulDiscoveryOptions> _optionsMonitor;
        private readonly ConsulDiscoveryOptions _options;

        public string Id => "consul";

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
        /// Initializes a new instance of the <see cref="ConsulHealthContributor"/> class.
        /// </summary>
        /// <param name="client">a Consul client to use for health checks</param>
        /// <param name="options">configuration options</param>
        /// <param name="logger">optional logger</param>
        public ConsulHealthContributor(IConsulClient client, ConsulDiscoveryOptions options, ILogger<ConsulHealthContributor> logger = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsulHealthContributor"/> class.
        /// </summary>
        /// <param name="client">a Consul client to use for health checks</param>
        /// <param name="options">configuration options</param>
        /// <param name="logger">optional logger</param>
        public ConsulHealthContributor(IConsulClient client, IOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor, ILogger<ConsulHealthContributor> logger = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
            _logger = logger;
        }

        /// <summary>
        /// Compute the health of the Consul server connection
        /// </summary>
        /// <returns>the health check result</returns>
        public HealthCheckResult Health()
        {
            var result = new HealthCheckResult();
            var leaderStatus = GetLeaderStatusAsync().Result;
            var services = GetCatalogServicesAsync().Result;
            result.Status = HealthStatus.UP;
            result.Details.Add("leader", leaderStatus);
            result.Details.Add("services", services);
            return result;
        }

        internal async Task<string> GetLeaderStatusAsync()
        {
            var result = await _client.Status.Leader().ConfigureAwait(false);
            return result;
        }

        internal async Task<Dictionary<string, string[]>> GetCatalogServicesAsync()
        {
            var result = await _client.Catalog.Services(QueryOptions.Default).ConfigureAwait(false);
            return result.Response;
        }
    }
}
