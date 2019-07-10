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
using HealthCheckResult = Steeltoe.Common.HealthChecks.HealthCheckResult;

namespace Steeltoe.Management.Endpoint.Health
{
    public class HealthEndpointCore : HealthEndpoint
    {
        private readonly IOptionsMonitor<HealthCheckServiceOptions> _serviceOptions;
        private readonly IServiceProvider _provider;
        private readonly IHealthAggregator _aggregator;
        private readonly IList<IHealthContributor> _contributors;
        private readonly ILogger<HealthEndpoint> _logger;

        public HealthEndpointCore(IHealthOptions options, IHealthAggregator aggregator, IEnumerable<IHealthContributor> contributors, IOptionsMonitor<HealthCheckServiceOptions> serviceOptions, IServiceProvider provider, ILogger<HealthEndpoint> logger = null)
            : base(options, aggregator, contributors, logger)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (contributors == null)
            {
                throw new ArgumentNullException(nameof(contributors));
            }

            _aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));
            _serviceOptions = serviceOptions ?? throw new ArgumentNullException(nameof(serviceOptions));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _contributors = contributors.ToList();
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
            return BuildHealth(_aggregator, _contributors, securityContext, _serviceOptions, _provider);
        }

        protected virtual HealthCheckResult BuildHealth(IHealthAggregator aggregator, IList<IHealthContributor> contributors, ISecurityContext securityContext, IOptionsMonitor<HealthCheckServiceOptions> svcOptions, IServiceProvider provider)
        {
            var registrationAggregator = _aggregator as IHealthRegistrationsAggregator;

            var result = registrationAggregator == null
                ? _aggregator.Aggregate(contributors)
                : registrationAggregator.Aggregate(contributors, svcOptions, provider);

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
