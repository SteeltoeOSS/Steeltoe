// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.HeapDump;

public class HeapDumpEndpointOptions// : AbstractEndpointOptions, IHeapDumpOptions
{
    private const string ManagementInfoPrefix = "management:endpoints:heapdump";

    public string HeapDumpType { get; set; }

    // Default to disabled on Linux + Cloud Foundry until PTRACE is allowed
    public bool DefaultEnabled { get; } = !(Platform.IsCloudFoundry && Platform.IsLinux);
    public EndpointSharedOptions EndpointOptions { get; set; }

    public HeapDumpEndpointOptions()
    {
        EndpointOptions = new EndpointSharedOptions
        {
            Id = "heapdump"
        };
    }

    //public HeapDumpEndpointOptions(IConfiguration configuration)
    //    : base(ManagementInfoPrefix, configuration)
    //{
    //    if (string.IsNullOrEmpty(Id))
    //    {
    //        Id = "heapdump";
    //    }
    //}
}
