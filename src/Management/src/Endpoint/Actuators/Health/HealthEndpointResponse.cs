// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Management.Endpoint.Actuators.Health;

public sealed class HealthEndpointResponse
{
    /// <summary>
    /// Gets the status of the health check.
    /// </summary>
    public HealthStatus Status { get; init; }

    /// <summary>
    /// Gets a description of the health check result.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets details of the health check.
    /// </summary>
    public IDictionary<string, object> Details { get; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets the list of available health groups.
    /// </summary>
    public IList<string> Groups { get; } = new List<string>();

    /// <summary>
    /// Gets a value indicating whether a health response exists.
    /// </summary>
    public bool Exists { get; init; } = true;

    public HealthEndpointResponse()
    {
    }

    public HealthEndpointResponse(HealthCheckResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        Status = result.Status;
        Description = result.Description;

        foreach ((string key, object value) in result.Details)
        {
            Details[key] = value;
        }
    }
}
