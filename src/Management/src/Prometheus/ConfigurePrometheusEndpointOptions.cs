// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Prometheus;

internal class ConfigurePrometheusEndpointOptions : ConfigureEndpointOptions<PrometheusEndpointOptions>
{
    internal const string ManagementInfoPrefix = "management:endpoints:prometheus";

    public ConfigurePrometheusEndpointOptions(IConfiguration configuration)
        : base(configuration, ManagementInfoPrefix, "prometheus")
    {
    }
}
