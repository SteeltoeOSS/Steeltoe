// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            var result = !(_aggregator is IHealthRegistrationsAggregator registrationAggregator)
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
