// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.ThreadDump;

public class ThreadDumpEndpointOptions : AbstractEndpointOptions, IThreadDumpOptions
{
    private const string ManagementInfoPrefix = "management:endpoints:dump";

    public int Duration { get; set; } = 10; // 10 ms

    public ThreadDumpEndpointOptions()
    {
        Id = "dump";
    }

    public ThreadDumpEndpointOptions(IConfiguration configuration)
        : base(ManagementInfoPrefix, configuration)
    {
        if (string.IsNullOrEmpty(Id))
        {
            Id = "dump";
        }
    }
}
