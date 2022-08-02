// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.HeapDump;

public class HeapDumpEndpointOptions : AbstractEndpointOptions, IHeapDumpOptions
{
    private const string ManagementInfoPrefix = "management:endpoints:heapdump";

    public string HeapDumpType { get; set; }

    // Default to disabled on Linux + Cloud Foundry until PTRACE is allowed
    public override bool DefaultEnabled { get; } = !(Platform.IsCloudFoundry && Platform.IsLinux);

    public HeapDumpEndpointOptions()
    {
        Id = "heapdump";
    }

    public HeapDumpEndpointOptions(IConfiguration config)
        : base(ManagementInfoPrefix, config)
    {
        if (string.IsNullOrEmpty(Id))
        {
            Id = "heapdump";
        }
    }
}
