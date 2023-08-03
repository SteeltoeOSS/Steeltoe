// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;

#pragma warning disable S4004 // Collection properties should be readonly

namespace Steeltoe.Management.Endpoint.Health;

public sealed class HealthEndpointResponse : HealthCheckResult
{
    /// <summary>
    /// Gets or sets the list of available health groups.
    /// </summary>
    public IList<string> Groups { get; set; } = new List<string>();

    public HealthEndpointResponse(HealthCheckResult? result)
    {
        result ??= new HealthCheckResult();

        Description = result.Description;
        Details = result.Details;
        Status = result.Status;
    }
}
