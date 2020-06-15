// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Hypermedia;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.CloudFoundry.Test
{
    internal class TestCloudFoundryEndpoint : CloudFoundryEndpoint
    {
        public TestCloudFoundryEndpoint(ICloudFoundryOptions options, IEnumerable<IManagementOptions> mgmtOpts, ILogger<CloudFoundryEndpoint> logger = null)
            : base(options, mgmtOpts, logger)
        {
        }

        public override Links Invoke(string baseUrl)
        {
            return new Links();
        }
    }
}