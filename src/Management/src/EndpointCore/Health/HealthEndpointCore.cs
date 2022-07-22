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

namespace Steeltoe.Management.Endpoint.Health;

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
        _aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));
        _serviceOptions = serviceOptions ?? throw new ArgumentNullException(nameof(serviceOptions));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _contributors = contributors.ToList();
        _logger = logger;
    }

    public new IHealthOptions Options => options as IHealthOptions;

    public override HealthEndpointResponse Invoke(ISecurityContext securityContext)
    {
        return BuildHealth(securityContext);
    }

    protected override HealthEndpointResponse BuildHealth(ISecurityContext securityContext)
    {
        var groupName = GetRequestedHealthGroup(securityContext);
        ICollection<HealthCheckRegistration> healthCheckRegistrations;
        IList<IHealthContributor> filteredContributors;
        if (!string.IsNullOrEmpty(groupName) && groupName != Options.Id)
        {
            filteredContributors = GetFilteredContributorList(groupName, _contributors);
            healthCheckRegistrations = GetFilteredHealthCheckServiceOptions(groupName, _serviceOptions);
        }
        else
        {
            filteredContributors = _contributors;
            healthCheckRegistrations = _serviceOptions.CurrentValue.Registrations;
        }

        var result = _aggregator is not IHealthRegistrationsAggregator registrationAggregator
            ? _aggregator.Aggregate(filteredContributors)
            : registrationAggregator.Aggregate(filteredContributors, healthCheckRegistrations, _provider);
        var response = new HealthEndpointResponse(result);

        var showDetails = Options.ShowDetails;
        if (showDetails == ShowDetails.Never || (showDetails == ShowDetails.WhenAuthorized && !securityContext.HasClaim(Options.Claim)))
        {
            response.Details = new Dictionary<string, object>();
        }
        else
        {
            response.Groups = Options.Groups.Select(g => g.Key);
        }

        return response;
    }

    private ICollection<HealthCheckRegistration> GetFilteredHealthCheckServiceOptions(string requestedGroup, IOptionsMonitor<HealthCheckServiceOptions> svcOptions)
    {
        if (!string.IsNullOrEmpty(requestedGroup))
        {
            if (Options.Groups.TryGetValue(requestedGroup, out var groupOptions))
            {
                var includedContributors = groupOptions.Include.Split(",").ToList();
                return svcOptions.CurrentValue.Registrations.Where(n => includedContributors.Contains(n.Name, StringComparer.InvariantCultureIgnoreCase)).ToList();
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

        return svcOptions.CurrentValue.Registrations;
    }
}