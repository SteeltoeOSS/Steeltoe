// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Health;

public class HealthEndpointResponse : HealthCheckResult
{
    public HealthEndpointResponse(HealthCheckResult result)
    {
        result ??= new HealthCheckResult();
        Description = result.Description;
        Details = result.Details;
        Components = result.HealthCheckResults.Select(healthResult => new HealthComponent()
        {
            Details = healthResult.Value.Details,
            Status = healthResult.Value.Status,
            Name = healthResult.Key
        }).ToDictionary(component => component.Name, component => component);

        Status = result.Status;
    }

    [JsonPropertyOrder(4)]
    public Dictionary<string, HealthComponent> Components { get; set; }

    /// <summary>
    /// Gets or sets the list of available health groups
    /// </summary>
    [JsonPropertyOrder(5)]
    public IEnumerable<string> Groups { get; set; }
}
