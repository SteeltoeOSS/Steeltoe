// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;
using System.IO;

namespace Steeltoe.Management.Endpoint.Health.Contributor;

public class PingHealthContributor : IHealthContributor
{
    private const string ID = "ping";

    public string Id { get; } = ID;

    public HealthCheckResult Health() => new HealthCheckResult() { Status = HealthStatus.UP, Details = null };
}