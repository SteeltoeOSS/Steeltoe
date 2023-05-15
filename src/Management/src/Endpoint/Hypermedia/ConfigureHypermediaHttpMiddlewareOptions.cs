// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Hypermedia;

internal sealed class ConfigureHypermediaHttpMiddlewareOptions : ConfigureMiddlewareOptions<HypermediaHttpMiddlewareOptions>
{
    private const string ManagementInfoPrefix = "management:endpoints:actuator";

    public ConfigureHypermediaHttpMiddlewareOptions(IConfiguration configuration)
        : base(configuration, ManagementInfoPrefix, string.Empty)
    {
    }
}
