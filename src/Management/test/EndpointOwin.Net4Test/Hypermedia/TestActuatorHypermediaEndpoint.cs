// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Hypermedia;
using System.Collections.Generic;

namespace Steeltoe.Management.EndpointOwin.Hypermedia.Test
{
    internal class TestActuatorHypermediaEndpoint : ActuatorEndpoint
    {
        public TestActuatorHypermediaEndpoint(IActuatorHypermediaOptions options, List<IManagementOptions> mgmtOptions, ILogger<ActuatorEndpoint> logger = null)
            : base(options, mgmtOptions, logger)
        {
        }

#pragma warning disable CS0618 // Type or member is obsolete
        public override Links Invoke(string baseUrl)
        {
            return new Links();
        }
#pragma warning restore CS0618 // Type or member is obsolete
    }
}