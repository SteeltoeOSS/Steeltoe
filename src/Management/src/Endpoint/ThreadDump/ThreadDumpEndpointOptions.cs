// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.ThreadDump;

public class ThreadDumpEndpointOptions : EndpointOptionsBase //, IThreadDumpOptions
{

    public int Duration { get; set; } = 10; // 10 ms


    //public ThreadDumpEndpointOptions(IConfiguration configuration)
    //    : base(ManagementInfoPrefix, configuration)
    //{
    //    if (string.IsNullOrEmpty(Id))
    //    {
    //        Id = "dump";
    //    }
    //}
}