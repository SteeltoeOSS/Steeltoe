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

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HealthCheckResult = Steeltoe.Common.HealthChecks.HealthCheckResult;

namespace Steeltoe.Management.Endpoint.Health
{
    public class HealthEndpointCore : HealthEndpoint
    {
        private readonly IHealthRegistrationsAggregator _registrationsAggregator;
        private readonly IOptionsMonitor<HealthCheckServiceOptions> _serviceOptions;
        private readonly IServiceProvider _provider;
        private readonly ILogger<HealthEndpoint> _logger;

        public HealthEndpointCore(IHealthOptions options, IHealthAggregator aggregator, IEnumerable<IHealthContributor> contributors, IOptionsMonitor<HealthCheckServiceOptions> serviceOptions, IServiceProvider provider, ILogger<HealthEndpoint> logger = null)
            : base(options, aggregator, contributors, logger)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _serviceOptions = serviceOptions ?? throw new ArgumentNullException(nameof(serviceOptions));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _logger = logger;
        }

        public HealthEndpointCore(IHealthOptions options, IAsyncHealthAggregator aggregator, IEnumerable<IAsyncHealthContributor> asyncContributors, IOptionsMonitor<HealthCheckServiceOptions> serviceOptions, IServiceProvider provider, ILogger<HealthEndpoint> logger = null)
          : base(options, aggregator, asyncContributors, logger)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _serviceOptions = serviceOptions ?? throw new ArgumentNullException(nameof(serviceOptions));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _logger = logger;
        }

        public HealthEndpointCore(IHealthOptions options, IHealthRegistrationsAggregator registrationsAggregator, IEnumerable<IAsyncHealthContributor> asyncContributors, IOptionsMonitor<HealthCheckServiceOptions> serviceOptions, IServiceProvider provider, ILogger<HealthEndpoint> logger = null)
        : base(options, null, asyncContributors, logger)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _registrationsAggregator = registrationsAggregator;
            _serviceOptions = serviceOptions ?? throw new ArgumentNullException(nameof(serviceOptions));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _logger = logger;
        }

        public new IHealthOptions Options
        {
            get
            {
                return options as IHealthOptions;
            }
        }

        public override HealthCheckResult Invoke(ISecurityContext securityContext)
        {
            return BuildHealth(securityContext, _serviceOptions, _provider).Result;
        }

        protected virtual async Task<HealthCheckResult> BuildHealth(ISecurityContext securityContext, IOptionsMonitor<HealthCheckServiceOptions> svcOptions, IServiceProvider provider)
        {
            HealthCheckResult result;

            if (_registrationsAggregator == null)
            {
                if (_aggregator != null)
                {
                    result = _aggregator.Aggregate(_contributors.ToList());
                }
                else
                {
                    result = await _asyncAggregator.Aggregate(_asyncContributors.ToList());
                }
            }
            else
            {
                result = await _registrationsAggregator.Aggregate(_contributors, _asyncContributors, svcOptions, provider);
            }

            var showDetails = Options.ShowDetails;

            if (showDetails == ShowDetails.Never
                || (showDetails == ShowDetails.WhenAuthorized
                      && !securityContext.HasClaim(Options.Claim)))
            {
                result = new HealthCheckResult
                {
                    Status = result.Status,
                    Description = result.Description
                };
            }

            return result;
        }
    }
}
