﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Management.Endpoint.Health.Test
{
    public class UnknownContributor : IHealthContributor
    {
        public string Id { get; } = "Unknown";

        public HealthCheckResult Health()
        {
            return new HealthCheckResult()
            {
                Status = HealthStatus.UNKNOWN
            };
        }
    }
}
