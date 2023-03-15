// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Security;

namespace Steeltoe.Management.Endpoint.Test.Health;

internal sealed class TestHealthEndpoint : HealthEndpointCore
{
    public TestHealthEndpoint(IOptionsMonitor<HealthEndpointOptions> options, IHealthAggregator aggregator, IEnumerable<IHealthContributor> contributors,
        IOptionsMonitor<HealthCheckServiceOptions> serviceOptions, IServiceProvider provider, ILogger<HealthEndpointCore> logger = null)
        : base(options, aggregator, contributors, serviceOptions, provider, logger)
    {
    }

    public override HealthEndpointResponse Invoke(ISecurityContext securityContext)
    {
        return new HealthEndpointResponse(null);
    }
}
