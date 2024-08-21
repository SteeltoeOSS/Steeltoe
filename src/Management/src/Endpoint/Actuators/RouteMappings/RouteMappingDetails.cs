// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Actuators.RouteMappings;

public sealed class RouteMappingDetails
{
    [JsonPropertyName("requestMappingConditions")]
    public RequestMappingConditions RequestMappingConditions { get; }

    public RouteMappingDetails(RequestMappingConditions requestMappingConditions)
    {
        ArgumentNullException.ThrowIfNull(requestMappingConditions);

        RequestMappingConditions = requestMappingConditions;
    }
}
