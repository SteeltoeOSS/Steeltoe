// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Management.Endpoint.Actuators.Health.Availability;

public sealed class LivenessHealthContributor : AvailabilityHealthContributor
{
    private readonly ApplicationAvailability _availability;
    private readonly IOptionsMonitor<LivenessHealthContributorOptions> _optionsMonitor;

    protected override bool IsEnabled => _optionsMonitor.CurrentValue.Enabled;

    public override string Id => "liveness";

    public LivenessHealthContributor(ApplicationAvailability availability, IOptionsMonitor<LivenessHealthContributorOptions> optionsMonitor,
        ILoggerFactory loggerFactory)
        : base(new Dictionary<AvailabilityState, HealthStatus>
        {
            { LivenessState.Correct, HealthStatus.Up },
            { LivenessState.Broken, HealthStatus.Down }
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
