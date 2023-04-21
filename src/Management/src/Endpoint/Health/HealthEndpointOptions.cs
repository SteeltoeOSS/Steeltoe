// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Steeltoe.Management.Endpoint.Security;

namespace Steeltoe.Management.Endpoint.Health;

public sealed class HealthEndpointOptions : EndpointOptionsBase
{
    public ShowDetails ShowDetails { get; set; }

    public EndpointClaim Claim { get; set; }

    public string Role { get; set; }

    [SuppressMessage("Major Code Smell", "S4004:Collection properties should be readonly", Justification = "Allow in Options")]
    public Dictionary<string, HealthGroupOptions> Groups { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public override bool ExactMatch => false;
}
