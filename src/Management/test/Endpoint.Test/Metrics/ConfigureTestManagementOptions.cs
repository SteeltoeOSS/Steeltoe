// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Test.Metrics;

internal sealed class ConfigureTestManagementOptions : ConfigureManagementEndpointOptions
{
    public ConfigureTestManagementOptions(IConfiguration configuration, IEnumerable<HttpMiddlewareOptions> endpointsCollection)
        : base(configuration, endpointsCollection)
    {
    }

    public override void Configure(string name, ManagementEndpointOptions options)
    {
        base.Configure(name, options);
        options.EndpointContexts |= EndpointContexts.CloudFoundry;
    }
}
