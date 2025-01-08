// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Health;

public sealed class HealthEndpointOptions : EndpointOptions
{
    public ShowDetails ShowDetails { get; set; } = ShowDetails.Never;
    public EndpointClaim? Claim { get; set; }
    public string? Role { get; set; }
    public IDictionary<string, HealthGroupOptions> Groups { get; } = new Dictionary<string, HealthGroupOptions>(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override bool RequiresExactMatch()
    {
        return false;
    }
}
