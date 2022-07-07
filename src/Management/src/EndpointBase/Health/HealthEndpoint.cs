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
    public class HealthEndpoint : AbstractEndpoint<HealthEndpointResponse, ISecurityContext>, IHealthEndpoint
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

        public override HealthEndpointResponse Invoke(ISecurityContext securityContext) => BuildHealth(securityContext);

        public int GetStatusCode(HealthCheckResult health)
        {
            return health.Status == HealthStatus.DOWN || health.Status == HealthStatus.OUT_OF_SERVICE
                ? 503
                : 200;
        }

        protected virtual HealthEndpointResponse BuildHealth(ISecurityContext securityContext)
        {
            var groupName = GetRequestedHealthGroup(securityContext);
            IList<IHealthContributor> filteredContributors;
            if (!string.IsNullOrEmpty(groupName))
            {
                filteredContributors = GetFilteredContributorList(groupName, _contributors);
            }
            else
            {
                filteredContributors = _contributors;
            }

            var healthCheckResult = _aggregator.Aggregate(filteredContributors);

            return GetHealthEndpointResponse(healthCheckResult, securityContext);
        }

        protected HealthEndpointResponse GetHealthEndpointResponse(HealthCheckResult healthCheckResult, ISecurityContext securityContext)
        {
            var hideDetails = Options.ShowDetails == ShowDetails.Never ||
                              (Options.ShowDetails == ShowDetails.WhenAuthorized &&
                               !securityContext.HasClaim(Options.Claim));
            var result = new HealthEndpointResponse(healthCheckResult);

            if (hideDetails)
            {
                result.Details = null;
                result.Components = null;
            }
            else
            {
                switch (securityContext?.GetMediaType() ?? Options.DefaultVersion)
                {
                    case MediaTypeVersion.V3:
                        result.Details = null;
                        break;
                    default:
                        result.Components = null;
                        break;
                }

                result.Groups = Options.Groups.Select(g => g.Key);
            }

            return result;
        }

        /// <summary>
        /// Returns the last value returned by <see cref="ISecurityContext.GetRequestComponents()"/>, expected to be the name of a configured health group
        /// </summary>
        /// <param name="securityContext">Last value of <see cref="ISecurityContext.GetRequestComponents()"/> is used as group name</param>
        protected string GetRequestedHealthGroup(ISecurityContext securityContext)
        {
            var requestComponents = securityContext?.GetRequestComponents();
            if (requestComponents != null && requestComponents.Length > 0)
            {
                return requestComponents[^1];
            }
            else
            {
                _logger?.LogWarning("Failed to find anything in the request from which to parse health group name.");
            }

            return string.Empty;
        }

        /// <summary>
        /// Filter out health contributors that do not belong to the requested group
        /// </summary>
        /// <param name="requestedGroup">Name of group from request</param>
        /// <param name="contributors">Full list of <see cref="IHealthContributor"/>s</param>
        /// <returns>
        ///     If the group is configured, returns health contributors that belong to the group. <para />
        ///     If group can't be parsed or is not configured, returns all health contributors.
        /// </returns>
        protected IList<IHealthContributor> GetFilteredContributorList(string requestedGroup, IList<IHealthContributor> contributors)
        {
            if (!string.IsNullOrEmpty(requestedGroup))
            {
                if (Options.Groups.TryGetValue(requestedGroup, out var groupOptions))
                {
                    var includedContributors = groupOptions.Include.Split(",").ToList();
                    contributors = contributors.Where(n => includedContributors.Contains(n.Id, StringComparer.InvariantCultureIgnoreCase)).ToList();
                }
                else
                {
                    _logger?.LogInformation("Health check requested for a group that is not configured");
                }
            }
            else
            {
                _logger?.LogTrace("Health group name not found in request");
            }

            return contributors;
        }
    }
}
