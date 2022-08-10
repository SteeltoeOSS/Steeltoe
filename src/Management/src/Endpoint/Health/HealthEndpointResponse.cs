// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Management.Endpoint.Health;

public class HealthEndpointResponse : HealthCheckResult
{
    /// <summary>
    /// Gets or sets the list of available health groups.
    /// </summary>
    public IEnumerable<string> Groups { get; set; }

    public HealthEndpointResponse(HealthCheckResult result)
    {
        result ??= new HealthCheckResult();
        Description = result.Description;
        Details = result.Details;
        Status = result.Status;
    }
}
