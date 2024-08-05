// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Health;

public sealed class HealthEndpointRequest
{
    public string GroupName { get; }
    public bool HasClaim { get; }

    public HealthEndpointRequest(string groupName, bool hasClaim)
    {
        ArgumentNullException.ThrowIfNull(groupName);

        GroupName = groupName;
        HasClaim = hasClaim;
    }
}
