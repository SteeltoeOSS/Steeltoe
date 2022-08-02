// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.HealthChecks;

/// <summary>
/// Implement this interface and add to DI to be included in health checks.
/// </summary>
public interface IHealthContributor
{
    /// <summary>
    /// Gets an identifier for the type of check being performed.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Check the health of a resource.
    /// </summary>
    /// <returns>
    /// The result of checking the health of a resource.
    /// </returns>
    HealthCheckResult Health();
}
