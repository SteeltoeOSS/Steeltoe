// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Management.Endpoint.Hypermedia.Test;

internal class TestHypermediaEndpoint : ActuatorEndpoint
{
    public TestHypermediaEndpoint(IActuatorHypermediaOptions options, ActuatorManagementOptions mgmtOptions, ILogger<ActuatorEndpoint> logger = null)
        : base(options, mgmtOptions, logger)
    {
    }

    public override Links Invoke(string baseUrl)
    {
        return new Links();
    }
}
