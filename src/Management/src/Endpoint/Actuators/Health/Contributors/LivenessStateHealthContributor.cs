// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Actuators.Health.Availability;

namespace Steeltoe.Management.Endpoint.Actuators.Health.Contributors;

internal sealed class LivenessStateHealthContributor : AvailabilityStateHealthContributor
{
    private readonly ApplicationAvailability _availability;
    private readonly IOptionsMonitor<LivenessStateContributorOptions> _optionsMonitor;

    protected override bool IsEnabled => _optionsMonitor.CurrentValue.Enabled;

    public override string Id => "livenessState";

    public LivenessStateHealthContributor(ApplicationAvailability availability, IOptionsMonitor<LivenessStateContributorOptions> optionsMonitor,
        ILoggerFactory loggerFactory)
        : base(new Dictionary<AvailabilityState, HealthStatus>
        {
            [LivenessState.Correct] = HealthStatus.Up,
            [LivenessState.Broken] = HealthStatus.Down
        }, loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(availability);
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        _availability = availability;
        _optionsMonitor = optionsMonitor;
    }

    protected override AvailabilityState? GetState()
    {
        return _availability.GetLivenessState();
    }
}
