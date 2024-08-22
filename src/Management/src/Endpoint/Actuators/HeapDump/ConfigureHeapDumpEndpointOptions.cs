// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.HeapDump;

internal sealed class ConfigureHeapDumpEndpointOptions : ConfigureEndpointOptions<HeapDumpEndpointOptions>
{
    private const string ManagementInfoPrefix = "management:endpoints:heapdump";
    private readonly ILogger<ConfigureHeapDumpEndpointOptions> _logger;

    public ConfigureHeapDumpEndpointOptions(IConfiguration configuration, ILogger<ConfigureHeapDumpEndpointOptions> logger)
        : base(configuration, ManagementInfoPrefix, "heapdump")
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    public override void Configure(HeapDumpEndpointOptions options)
    {
        base.Configure(options);

        // Only gcdump works on OSX
        if (Platform.IsOSX && options.HeapDumpType != "gcdump")
        {
            _logger.LogWarning("Only GC dumps are supported on OSX.");
            options.HeapDumpType = "gcdump";
        }
    }
}
