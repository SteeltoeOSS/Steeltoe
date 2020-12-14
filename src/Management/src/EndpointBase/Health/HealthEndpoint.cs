// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using HealthCheckResult = Steeltoe.Common.HealthChecks.HealthCheckResult;
using HealthStatus = Steeltoe.Common.HealthChecks.HealthStatus;

namespace Steeltoe.Management.Endpoint.Health
{
    // Note: this is not used by EndpointCore (ASP.NET Core apps) -- see also HealthEndpointCore.cs
    public class HealthEndpoint : AbstractEndpoint<HealthCheckResult, ISecurityContext>
    {
        private readonly IHealthAggregator _aggregator;
        private readonly IList<IHealthContributor> _contributors;
        private readonly ILogger<HealthEndpoint> _logger;

        public HealthEndpoint(IHealthOptions options, IHealthAggregator aggregator, IEnumerable<IHealthContributor> contributors, ILogger<HealthEndpoint> logger = null)
           : base(options)
        {
            if (contributors == null)
            {
                throw new ArgumentNullException(nameof(contributors));
            }

            _aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));
            _contributors = contributors.ToList();
            _logger = logger;
        }

        public new IHealthOptions Options => options as IHealthOptions;

        public override HealthCheckResult Invoke(ISecurityContext securityContext) => BuildHealth(_aggregator, _contributors, securityContext);

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
