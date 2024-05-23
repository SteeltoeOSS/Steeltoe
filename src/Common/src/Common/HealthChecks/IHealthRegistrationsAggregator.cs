// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Steeltoe.Common.HealthChecks;

public interface IHealthRegistrationsAggregator : IHealthAggregator
{
    Task<HealthCheckResult> AggregateAsync(ICollection<IHealthContributor> contributors, ICollection<HealthCheckRegistration> healthCheckRegistrations,
        IServiceProvider serviceProvider, CancellationToken cancellationToken);
}
