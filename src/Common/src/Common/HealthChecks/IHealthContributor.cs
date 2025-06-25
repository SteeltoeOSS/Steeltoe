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
    /// Performs a health check.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// The result of the health check, or <c>null</c> if this health check is currently disabled.
    /// </returns>
    Task<HealthCheckResult?> CheckHealthAsync(CancellationToken cancellationToken);
}
