// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Security;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Health.Test
{
    internal class TestHealthEndpoint : HealthEndpoint
    {
        public TestHealthEndpoint(IHealthOptions options, IHealthAggregator aggregator, IEnumerable<IHealthContributor> contributors, ILogger<HealthEndpoint> logger = null)
            : base(options, aggregator, contributors, logger)
        {
        }

        public override HealthCheckResult Invoke(ISecurityContext securityContext)
        {
            return new HealthCheckResult();
        }
    }
}
