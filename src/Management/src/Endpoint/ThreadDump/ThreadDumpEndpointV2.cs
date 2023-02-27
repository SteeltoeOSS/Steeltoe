// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.ThreadDump;

public class ThreadDumpEndpointV2 : IEndpoint<ThreadDumpResult>, IThreadDumpEndpointV2
{
    private readonly IThreadDumper _threadDumper;

    //public new IThreadDumpOptions Options => options as IThreadDumpOptions;

    public ThreadDumpEndpointV2(IOptionsMonitor<ThreadDumpEndpointOptions> options, IThreadDumper threadDumper, ILogger<ThreadDumpEndpointV2> logger = null)
       // : base(options)
    {
        ArgumentGuard.NotNull(threadDumper);
        Options = options;
        _threadDumper = threadDumper;
    }

    public IOptionsMonitor<ThreadDumpEndpointOptions> Options { get; }

    IEndpointOptions IEndpoint.Options => Options.CurrentValue;

    public ThreadDumpResult Invoke()
    {
        return new ThreadDumpResult
        {
            Threads = _threadDumper.DumpThreads()
        };
    }
}
