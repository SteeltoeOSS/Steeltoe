// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Common.HealthChecks;

/// <summary>
/// The result of a health check
/// </summary>
public class HealthCheckResult
{
    /// <summary>
    /// Gets or sets the status of the check
    /// </summary>
    /// <remarks>Used by HealthMiddleware to determine HTTP Status code</remarks>
    public HealthStatus Status { get; set; } = HealthStatus.UNKNOWN;

    /// <summary>
    /// Gets or sets a description of the health check result
    /// </summary>
    /// <remarks>Currently only used on check failures</remarks>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets details of the checked item
    /// </summary>
    /// <remarks>For parity with Spring Boot, repeat status [with a call to .ToString()] here</remarks>
    public Dictionary<string, object> Details { get; set; } = new ();
}
