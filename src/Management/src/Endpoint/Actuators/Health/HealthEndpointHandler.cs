// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Configuration;
using HealthCheckResult = Steeltoe.Common.HealthChecks.HealthCheckResult;
using HealthStatus = Steeltoe.Common.HealthChecks.HealthStatus;

namespace Steeltoe.Management.Endpoint.Actuators.Health;

internal sealed class HealthEndpointHandler : IHealthEndpointHandler
{
    private readonly IOptionsMonitor<HealthEndpointOptions> _endpointOptionsMonitor;
    private readonly IHealthAggregator _healthAggregator;
    private readonly IHealthContributor[] _healthContributors;
    private readonly IOptionsMonitor<HealthCheckServiceOptions> _healthOptionsMonitor;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HealthEndpointHandler> _logger;

    public EndpointOptions Options => _endpointOptionsMonitor.CurrentValue;

    public HealthEndpointHandler(IOptionsMonitor<HealthEndpointOptions> endpointOptionsMonitor, IHealthAggregator healthAggregator,
        IEnumerable<IHealthContributor> healthContributors, IOptionsMonitor<HealthCheckServiceOptions> healthOptionsMonitor, IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(endpointOptionsMonitor);
        ArgumentNullException.ThrowIfNull(healthAggregator);
        ArgumentNullException.ThrowIfNull(healthContributors);
        ArgumentNullException.ThrowIfNull(healthOptionsMonitor);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        IHealthContributor[] healthContributorArray = healthContributors.ToArray();
        ArgumentGuard.ElementsNotNull(healthContributorArray);

        _endpointOptionsMonitor = endpointOptionsMonitor;
        _healthAggregator = healthAggregator;
        _healthContributors = healthContributorArray;
        _healthOptionsMonitor = healthOptionsMonitor;
        _serviceProvider = serviceProvider;
        _logger = loggerFactory.CreateLogger<HealthEndpointHandler>();
    }

    public int GetStatusCode(HealthEndpointResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return response.Status is HealthStatus.Down or HealthStatus.OutOfService ? 503 : 200;
    }

    public async Task<HealthEndpointResponse> InvokeAsync(HealthEndpointRequest healthRequest, CancellationToken cancellationToken)
    {
        string groupName = healthRequest.GroupName;
        HealthEndpointOptions endpointOptions = _endpointOptionsMonitor.CurrentValue;

        IHealthContributor[] filteredContributors;
        ICollection<HealthCheckRegistration> healthCheckRegistrations;

        if (!string.IsNullOrEmpty(groupName) && groupName != endpointOptions.Id)
        {
            filteredContributors = GetFilteredContributors(groupName);
            healthCheckRegistrations = GetFilteredHealthCheckServiceOptions(groupName);
        }
        else
        {
            filteredContributors = _healthContributors;
            healthCheckRegistrations = _healthOptionsMonitor.CurrentValue.Registrations;
        }

        HealthCheckResult result = await _healthAggregator.AggregateAsync(filteredContributors, healthCheckRegistrations, _serviceProvider, cancellationToken);

        var response = new HealthEndpointResponse(result);

        ShowDetails showDetails = endpointOptions.ShowDetails;

        if (showDetails == ShowDetails.Never || (showDetails == ShowDetails.WhenAuthorized && !healthRequest.HasClaim))
        {
            response.Details.Clear();
        }
        else
        {
            foreach (string group in endpointOptions.Groups.Select(group => group.Key))
            {
                response.Groups.Add(group);
            }
        }

        return response;
    }

    private ICollection<HealthCheckRegistration> GetFilteredHealthCheckServiceOptions(string requestedGroup)
    {
        if (!string.IsNullOrEmpty(requestedGroup))
        {
            if (_endpointOptionsMonitor.CurrentValue.Groups.TryGetValue(requestedGroup, out HealthGroupOptions? groupOptions) && groupOptions.Include != null)
            {
                string[] includedContributors = groupOptions.Include.Split(',');

                return _healthOptionsMonitor.CurrentValue.Registrations
                    .Where(registration => includedContributors.Contains(registration.Name, StringComparer.OrdinalIgnoreCase)).ToArray();
            }

            _logger.LogInformation("Health check requested for a group that is not configured");
        }
        else
        {
            _logger.LogTrace("Health group name not found in request");
        }

        return _healthOptionsMonitor.CurrentValue.Registrations;
    }

    /// <summary>
    /// Filter out health contributors that do not belong to the requested group.
    /// </summary>
    /// <param name="requestedGroup">
    /// Name of group from request.
    /// </param>
    /// <returns>
    /// If the group is configured, returns health contributors that belong to the group.
    /// <para />
    /// If group can't be parsed or is not configured, returns all health contributors.
    /// </returns>
    private IHealthContributor[] GetFilteredContributors(string requestedGroup)
    {
        if (!string.IsNullOrEmpty(requestedGroup))
        {
            if (_endpointOptionsMonitor.CurrentValue.Groups.TryGetValue(requestedGroup, out HealthGroupOptions? groupOptions) && groupOptions.Include != null)
            {
                string[] includedContributors = groupOptions.Include.Split(',');
                return _healthContributors.Where(contributor => includedContributors.Contains(contributor.Id, StringComparer.OrdinalIgnoreCase)).ToArray();
            }

            _logger.LogInformation("Health check requested for a group that is not configured");
        }
        else
        {
            _logger.LogTrace("Health group name not found in request");
        }

        return _healthContributors;
    }
}
