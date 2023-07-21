// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using HealthCheckResult = Steeltoe.Common.HealthChecks.HealthCheckResult;
using HealthStatus = Steeltoe.Common.HealthChecks.HealthStatus;

namespace Steeltoe.Management.Endpoint.Health;

internal sealed class HealthEndpointHandler : IHealthEndpointHandler
{
    private readonly IOptionsMonitor<HealthCheckServiceOptions> _serviceOptions;
    private readonly IServiceProvider _provider;
    private readonly IOptionsMonitor<HealthEndpointOptions> _options;
    private readonly IHealthAggregator _aggregator;
    private readonly IList<IHealthContributor> _contributors;
    private readonly ILogger<HealthEndpointHandler> _logger;
    public HttpMiddlewareOptions Options => _options.CurrentValue;

    public HealthEndpointHandler(IOptionsMonitor<HealthEndpointOptions> options, IHealthAggregator aggregator, IEnumerable<IHealthContributor> contributors,
        IOptionsMonitor<HealthCheckServiceOptions> serviceOptions, IServiceProvider provider, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(aggregator);
        ArgumentGuard.NotNull(contributors);
        ArgumentGuard.NotNull(serviceOptions);
        ArgumentGuard.NotNull(provider);
        ArgumentGuard.NotNull(loggerFactory);

        _options = options;
        _aggregator = aggregator;
        _serviceOptions = serviceOptions;
        _provider = provider;
        _contributors = contributors.ToList();
        _logger = loggerFactory.CreateLogger<HealthEndpointHandler>();
    }

    public Task<HealthEndpointResponse> InvokeAsync(HealthEndpointRequest healthRequest, CancellationToken cancellationToken)
    {
        HealthEndpointResponse response = RunHealthChecks(healthRequest, cancellationToken);
        return Task.FromResult(response);
    }

    public int GetStatusCode(HealthCheckResult health)
    {
        ArgumentGuard.NotNull(health);
        return health.Status == HealthStatus.Down || health.Status == HealthStatus.OutOfService ? 503 : 200;
    }

    private HealthEndpointResponse RunHealthChecks(HealthEndpointRequest healthRequest, CancellationToken cancellationToken)
    {
        string groupName = healthRequest.GroupName;
        IEnumerable<HealthCheckRegistration> healthCheckRegistrations;
        IList<IHealthContributor> filteredContributors;
        HealthEndpointOptions options = _options.CurrentValue;

        if (!string.IsNullOrEmpty(groupName) && groupName != options.Id)
        {
            filteredContributors = GetFilteredContributorList(groupName, _contributors);
            healthCheckRegistrations = GetFilteredHealthCheckServiceOptions(groupName, _serviceOptions);
        }
        else
        {
            filteredContributors = _contributors;
            healthCheckRegistrations = _serviceOptions.CurrentValue.Registrations;
        }

        HealthCheckResult result = _aggregator is not IHealthRegistrationsAggregator registrationAggregator
            ? _aggregator.Aggregate(filteredContributors, cancellationToken)
            : registrationAggregator.Aggregate(filteredContributors, healthCheckRegistrations, _provider, cancellationToken);

        var response = new HealthEndpointResponse(result);

        ShowDetails showDetails = options.ShowDetails;

        if (showDetails == ShowDetails.Never || (showDetails == ShowDetails.WhenAuthorized && !healthRequest.HasClaim))
        {
            response.Details = new Dictionary<string, object>();
        }
        else
        {
            response.Groups = options.Groups.Select(g => g.Key);
        }

        return response;
    }

    private IEnumerable<HealthCheckRegistration> GetFilteredHealthCheckServiceOptions(string requestedGroup,
        IOptionsMonitor<HealthCheckServiceOptions> svcOptions)
    {
        if (!string.IsNullOrEmpty(requestedGroup))
        {
            if (_options.CurrentValue.Groups.TryGetValue(requestedGroup, out HealthGroupOptions groupOptions))
            {
                List<string> includedContributors = groupOptions.Include.Split(',').ToList();

                return svcOptions.CurrentValue.Registrations.Where(n => includedContributors.Contains(n.Name, StringComparer.OrdinalIgnoreCase)).ToList();
            }

            _logger.LogInformation("Health check requested for a group that is not configured");
        }
        else
        {
            _logger.LogTrace("Health group name not found in request");
        }

        return svcOptions.CurrentValue.Registrations;
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
    private IList<IHealthContributor> GetFilteredContributorList(string requestedGroup, IList<IHealthContributor> contributors)
    {
        if (!string.IsNullOrEmpty(requestedGroup))
        {
            if (_options.CurrentValue.Groups.TryGetValue(requestedGroup, out HealthGroupOptions groupOptions))
            {
                List<string> includedContributors = groupOptions.Include.Split(',').ToList();
                contributors = contributors.Where(n => includedContributors.Contains(n.Id, StringComparer.OrdinalIgnoreCase)).ToList();
            }
            else
            {
                _logger.LogInformation("Health check requested for a group that is not configured");
            }
        }
        else
        {
            _logger.LogTrace("Health group name not found in request");
        }

        return contributors;
    }
}
