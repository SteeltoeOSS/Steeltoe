// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Actuators.Health;

public sealed class HealthEndpointRequest
{
    [JsonPropertyName("groupName")]
    public string GroupName { get; }

    [JsonPropertyName("hasClaim")]
    public bool HasClaim { get; }

    public HealthEndpointRequest(string groupName, bool hasClaim)
    {
        ArgumentNullException.ThrowIfNull(groupName);

        GroupName = groupName;
        HasClaim = hasClaim;
    }
}
