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
        ArgumentNullException.ThrowIfNull(healthRequest);

        string groupName = healthRequest.GroupName;
        HealthEndpointOptions endpointOptions = _endpointOptionsMonitor.CurrentValue;

        HealthGroupOptions? groupOptions = GetHealthGroupOptions(groupName, endpointOptions);

        HealthEndpointResponse response = await GetResponseAsync(groupOptions, cancellationToken);

        CleanResult(groupOptions, healthRequest, response);

        if (string.IsNullOrEmpty(groupName))
        {
            foreach (string group in endpointOptions.Groups.Select(group => group.Key))
            {
                response.Groups.Add(group);
            }
        }

        return response;
    }

    private async Task<HealthEndpointResponse> GetResponseAsync(HealthGroupOptions? groupOptions, CancellationToken cancellationToken)
    {
        IHealthContributor[] filteredContributors = GetFilteredContributors(groupOptions);
        ICollection<HealthCheckRegistration> healthCheckRegistrations = GetFilteredRegistrations(groupOptions);

        HealthCheckResult result = await _healthAggregator.AggregateAsync(filteredContributors, healthCheckRegistrations, _serviceProvider, cancellationToken);

        return new HealthEndpointResponse(result);
    }

    private ICollection<HealthCheckRegistration> GetFilteredRegistrations(HealthGroupOptions? groupOptions)
    {
        if (groupOptions is { Include: not null })
        {
            string[] includedContributors = groupOptions.Include.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            return _healthOptionsMonitor.CurrentValue.Registrations
                .Where(contributor => includedContributors.Contains(contributor.Name, StringComparer.OrdinalIgnoreCase)).ToArray();
        }

        return _healthOptionsMonitor.CurrentValue.Registrations;
    }

    private IHealthContributor[] GetFilteredContributors(HealthGroupOptions? groupOptions)
    {
        if (groupOptions is { Include: not null })
        {
            string[] includedContributors = groupOptions.Include.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return _healthContributors.Where(contributor => includedContributors.Contains(contributor.Id, StringComparer.OrdinalIgnoreCase)).ToArray();
        }

        return _healthContributors;
    }

    private static HealthGroupOptions? GetHealthGroupOptions(string requestedGroup, HealthEndpointOptions endpointOptions)
    {
        return requestedGroup.Length > 0 && endpointOptions.Groups.TryGetValue(requestedGroup, out HealthGroupOptions? groupOptions) ? groupOptions : null;
    }

    private void CleanResult(HealthGroupOptions? groupOptions, HealthEndpointRequest healthRequest, HealthEndpointResponse response)
    {
        ShowValues showComponents = groupOptions?.ShowComponents ?? _endpointOptionsMonitor.CurrentValue.ShowComponents;

        if (ShouldClear(showComponents, healthRequest))
        {
            _logger.LogTrace("Clearing health check components. ShowComponents={ShowComponents}, HasClaim={HasClaimForHealth}.", showComponents,
                healthRequest.HasClaim);

            response.Components.Clear();
        }
        else
        {
            ShowValues showDetails = groupOptions?.ShowDetails ?? _endpointOptionsMonitor.CurrentValue.ShowDetails;

            if (response.Components.Any() && ShouldClear(showDetails, healthRequest))
            {
                _logger.LogTrace("Clearing health check component details. ShowDetails={ShowDetails}, HasClaim={HasClaimForHealth}.", showDetails,
                    healthRequest.HasClaim);

                foreach (KeyValuePair<string, HealthCheckResult> component in response.Components)
                {
                    component.Value.Details.Clear();
                }
            }
        }
    }

    private static bool ShouldClear(ShowValues showValues, HealthEndpointRequest healthRequest)
    {
        return showValues == ShowValues.Never || (showValues == ShowValues.WhenAuthorized && !healthRequest.HasClaim);
    }
}
