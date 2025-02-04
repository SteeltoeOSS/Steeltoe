// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Health;

public sealed class HealthEndpointOptions : EndpointOptions
{
    /// <summary>
    /// Gets or sets whether to show components in responses.
    /// </summary>
    public ShowValues ShowComponents { get; set; }

    /// <summary>
    /// Gets or sets whether to show details of components in responses.
    /// </summary>
    public ShowValues ShowDetails { get; set; }

    /// <summary>
    /// Gets or sets the claim requirements for retrieving components and/or details.
    /// </summary>
    public EndpointClaim? Claim { get; set; }

    /// <summary>
    /// Gets or sets the role required to retrieve components and/or details.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Gets the configured health groups.
    /// </summary>
    public IDictionary<string, HealthGroupOptions> Groups { get; } = new Dictionary<string, HealthGroupOptions>(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override bool RequiresExactMatch()
    {
        return false;
    }
}
