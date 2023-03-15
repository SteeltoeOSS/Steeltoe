// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Security;

namespace Steeltoe.Management.Endpoint.Health;

public class HealthEndpointOptions : EndpointOptionsBase
{
    public ShowDetails ShowDetails { get; set; }

    public EndpointClaim Claim { get; set; }

    public string Role { get; set; }

    public Dictionary<string, HealthGroupOptions> Groups { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public override bool ExactMatch => false;
}
