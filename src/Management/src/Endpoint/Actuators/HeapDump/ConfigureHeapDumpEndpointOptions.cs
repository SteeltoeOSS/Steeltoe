// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.HeapDump;

internal sealed class ConfigureHeapDumpEndpointOptions(IConfiguration configuration)
    : ConfigureEndpointOptions<HeapDumpEndpointOptions>(configuration, "Management:Endpoints:Heapdump", "heapdump")
{
    public override void Configure(HeapDumpEndpointOptions options)
    {
        base.Configure(options);

        options.HeapDumpType ??= Platform.IsOSX ? HeapDumpType.GCDump : HeapDumpType.Full;

        if (options.GCDumpTimeoutInSeconds < 1)
        {
            options.GCDumpTimeoutInSeconds = int.MaxValue;
        }
    }
}
