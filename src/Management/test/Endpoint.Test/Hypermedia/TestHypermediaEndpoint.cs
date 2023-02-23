// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Hypermedia;

namespace Steeltoe.Management.Endpoint.Test.Hypermedia;

internal sealed class TestHypermediaEndpoint : ActuatorEndpoint
{
    public TestHypermediaEndpoint(IOptionsMonitor<HypermediaEndpointOptions> options, IOptionsMonitor<ManagementEndpointOptions> managementOptions, ILogger<ActuatorEndpoint> logger = null)
        : base(options, managementOptions, logger)
    {
    }

    public override Links Invoke(string baseUrl)
    {
        return new Links();
    }
}
