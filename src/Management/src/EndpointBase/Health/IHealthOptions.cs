// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Security;

namespace Steeltoe.Management.Endpoint.Health;

public interface IHealthOptions : IEndpointOptions
{
    ShowDetails ShowDetails { get; set; }

    EndpointClaim Claim { get; set; }

    string Role { get; set; }

    public Dictionary<string, HealthGroupOptions> Groups { get; set; }
}
