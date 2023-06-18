// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Steeltoe.Management.Endpoint.Trace;

internal class ConfigureTraceEndpointOptions : IConfigureNamedOptions<TraceEndpointOptions>
{
    private const string ManagementInfoPrefixV1 = "management:endpoints:trace";
    private const string ManagementInfoPrefix = "management:endpoints:httptrace";
    private const int DefaultCapacity = 100;
    private readonly IConfiguration _configuration;

    public ConfigureTraceEndpointOptions(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Configure(string name, TraceEndpointOptions options)
    {
        if (name == TraceEndpointOptionNames.V2.ToString() || name == string.Empty)
        {
            Configure(options);
        }
        else
        {
            _configuration.GetSection(ManagementInfoPrefixV1).Bind(options);

            if (string.IsNullOrEmpty(options.Id))
            {
                options.Id = "trace";
            }
        }

        if (options.Capacity == -1)
        {
            options.Capacity = DefaultCapacity;
        }
    }

    public void Configure(TraceEndpointOptions options)
    {
        _configuration.GetSection(ManagementInfoPrefix).Bind(options);

        if (string.IsNullOrEmpty(options.Id))
        {
            options.Id = "httptrace";
        }
    }

    public enum TraceEndpointOptionNames
    {
        V1,
        V2
    }
}
