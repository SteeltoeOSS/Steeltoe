// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Test.CloudFoundry;

internal sealed class TestCloudFoundryEndpoint : CloudFoundryEndpoint
{
    public TestCloudFoundryEndpoint(IOptionsMonitor<CloudFoundryEndpointOptions> options, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        ILoggerFactory loggerFactory)
        : base(options, managementOptions, Enumerable.Empty<IEndpointOptions>(), loggerFactory)
    {
    }

    public override Links Invoke(string baseUrl)
    {
        return new Links();
    }
}
