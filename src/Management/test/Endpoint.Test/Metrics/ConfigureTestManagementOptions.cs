// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Test.Metrics;

internal class ConfigureTestManagementOptions : ConfigureManagementEndpointOptions
{
    public ConfigureTestManagementOptions(IConfiguration configuration, IEnumerable<IContextName> contextNames,
        IEnumerable<IEndpointOptions> endpointsCollection)
        : base(configuration, contextNames, endpointsCollection)
    {
    }

    public override void Configure(string name, ManagementEndpointOptions options)
    {
        base.Configure(name, options);
        options.ContextNames.Add(CFContext.Name);
    }
}
