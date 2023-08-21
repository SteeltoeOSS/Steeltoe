// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.HeapDump;

internal sealed class ConfigureHeapDumpEndpointOptions : ConfigureEndpointOptions<HeapDumpEndpointOptions>
{
    private readonly ILogger<ConfigureHeapDumpEndpointOptions> _logger;
    private const string ManagementInfoPrefix = "management:endpoints:heapdump";


    public ConfigureHeapDumpEndpointOptions(IConfiguration configuration, ILogger<ConfigureHeapDumpEndpointOptions> logger)
        : base(configuration, ManagementInfoPrefix, "heapdump")
    {
        _logger = logger;
    }

    public override void Configure(HeapDumpEndpointOptions options)
    {
        base.Configure(options);

        // Full dumps are broken on Osx, so default to gcdump
        if (Platform.IsOSX)
        {
            _logger.LogInformation("Full dumps are not supported on OSX, defaulting to gcdump.");
            options.HeapDumpType = "gcdump";
        }
    }
}
