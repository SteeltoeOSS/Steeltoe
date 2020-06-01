// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.CloudFoundry;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Hypermedia.Test
{
    internal class TestHypermediaEndpoint : ActuatorEndpoint
    {
        public TestHypermediaEndpoint(IActuatorHypermediaOptions options, IEnumerable<IManagementOptions> mgmtOptions, ILogger<ActuatorEndpoint> logger = null)
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