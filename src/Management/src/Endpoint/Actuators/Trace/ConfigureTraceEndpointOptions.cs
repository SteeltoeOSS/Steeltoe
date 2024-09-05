// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Trace;

internal sealed class ConfigureTraceEndpointOptions(IConfiguration configuration)
    : ConfigureEndpointOptions<TraceEndpointOptions>(configuration, ManagementInfoPrefixV2, EndpointIdV2), IConfigureNamedOptions<TraceEndpointOptions>
{
    private const string EndpointIdV1 = "trace";
    private const string EndpointIdV2 = "httptrace";
    private const string ManagementInfoPrefixV1 = "management:endpoints:trace";
    private const string ManagementInfoPrefixV2 = "management:endpoints:httptrace";
    private const int DefaultCapacity = 100;

    public void Configure(string? name, TraceEndpointOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (name == TraceEndpointOptionNames.V2.ToString() || string.IsNullOrEmpty(name))
        {
            ConfigureAtKey(Configuration, ManagementInfoPrefixV2, EndpointIdV2, options);
        }
        else
        {
            ConfigureAtKey(Configuration, ManagementInfoPrefixV1, EndpointIdV1, options);
        }

        if (options.Capacity == -1)
        {
            options.Capacity = DefaultCapacity;
        }
    }

    public enum TraceEndpointOptionNames
    {
        V1,
        V2
    }
}
