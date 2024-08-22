// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Metrics;

internal sealed class ConfigureMetricsEndpointOptions : ConfigureEndpointOptions<MetricsEndpointOptions>
{
    private const string ManagementInfoPrefix = "management:endpoints:metrics";

    public ConfigureMetricsEndpointOptions(IConfiguration configuration)
        : base(configuration, ManagementInfoPrefix, "metrics")
    {
    }
}
