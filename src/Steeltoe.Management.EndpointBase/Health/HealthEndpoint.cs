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

using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Security;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Endpoint.Health
{
    public class HealthEndpoint : AbstractEndpoint<HealthCheckResult, ISecurityContext>
    {
        private IHealthAggregator _aggregator;
        private IList<IHealthContributor> _contributors;
        private ILogger<HealthEndpoint> _logger;

        public HealthEndpoint(IHealthOptions options, IHealthAggregator aggregator, IEnumerable<IHealthContributor> contributors, ILogger<HealthEndpoint> logger = null)
           : base(options)
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

            return BuildHealth(_aggregator, _contributors, securityContext);
        }

        public int GetStatusCode(HealthCheckResult health)
        {
            return health.Status == HealthStatus.DOWN || health.Status == HealthStatus.OUT_OF_SERVICE
                ? 503
                : 200;
        }

        protected virtual HealthCheckResult BuildHealth(IHealthAggregator aggregator, IList<IHealthContributor> contributors, ISecurityContext securityContext)
        {
            var result = _aggregator.Aggregate(contributors);
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
