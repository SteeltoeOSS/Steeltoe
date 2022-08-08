// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Security;

namespace Steeltoe.Management.Endpoint.Health;

// Note: this is not used by EndpointCore (ASP.NET Core apps) -- see also HealthEndpointCore.cs
public class HealthEndpoint : AbstractEndpoint<HealthEndpointResponse, ISecurityContext>, IHealthEndpoint
{
    private readonly IHealthAggregator _aggregator;
    private readonly IList<IHealthContributor> _contributors;
    private readonly ILogger<HealthEndpoint> _logger;

    public new IHealthOptions Options => options as IHealthOptions;

    public HealthEndpoint(IHealthOptions options, IHealthAggregator aggregator, IEnumerable<IHealthContributor> contributors,
        ILogger<HealthEndpoint> logger = null)
        : base(options)
    {
        ArgumentGuard.NotNull(contributors);

        ArgumentGuard.NotNull(aggregator);

        _aggregator = aggregator;
        _contributors = contributors.ToList();
        _logger = logger;
    }

    public override HealthEndpointResponse Invoke(ISecurityContext securityContext)
    {
        return BuildHealth(securityContext);
    }

    public int GetStatusCode(HealthCheckResult health)
    {
        return health.Status == HealthStatus.Down || health.Status == HealthStatus.OutOfService ? 503 : 200;
    }

    protected virtual HealthEndpointResponse BuildHealth(ISecurityContext securityContext)
    {
        string groupName = GetRequestedHealthGroup(securityContext);

        IList<IHealthContributor> filteredContributors =
            !string.IsNullOrEmpty(groupName) ? GetFilteredContributorList(groupName, _contributors) : _contributors;

        var result = new HealthEndpointResponse(_aggregator.Aggregate(filteredContributors));

        ShowDetails showDetails = Options.ShowDetails;

        if (showDetails == ShowDetails.Never || (showDetails == ShowDetails.WhenAuthorized && !securityContext.HasClaim(Options.Claim)))
        {
            result.Details = new Dictionary<string, object>();
        }
        else
        {
            result.Groups = Options.Groups.Select(g => g.Key);
        }

        return result;
    }

    /// <summary>
    /// Returns the last value returned by <see cref="ISecurityContext.GetRequestComponents()" />, expected to be the name of a configured health group.
    /// </summary>
    /// <param name="securityContext">
    /// Last value of <see cref="ISecurityContext.GetRequestComponents()" /> is used as group name.
    /// </param>
    protected string GetRequestedHealthGroup(ISecurityContext securityContext)
    {
        string[] requestComponents = securityContext?.GetRequestComponents();

        if (requestComponents != null && requestComponents.Length > 0)
        {
            return requestComponents[^1];
        }

        _logger?.LogWarning("Failed to find anything in the request from which to parse health group name.");

        return string.Empty;
    }

    /// <summary>
    /// Filter out health contributors that do not belong to the requested group.
    /// </summary>
    /// <param name="requestedGroup">
    /// Name of group from request.
    /// </param>
    /// <param name="contributors">
    /// Full list of <see cref="IHealthContributor" />s.
    /// </param>
    /// <returns>
    /// If the group is configured, returns health contributors that belong to the group.
    /// <para />
    /// If group can't be parsed or is not configured, returns all health contributors.
    /// </returns>
    protected IList<IHealthContributor> GetFilteredContributorList(string requestedGroup, IList<IHealthContributor> contributors)
    {
        if (!string.IsNullOrEmpty(requestedGroup))
        {
            if (Options.Groups.TryGetValue(requestedGroup, out HealthGroupOptions groupOptions))
            {
                List<string> includedContributors = groupOptions.Include.Split(",").ToList();
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
