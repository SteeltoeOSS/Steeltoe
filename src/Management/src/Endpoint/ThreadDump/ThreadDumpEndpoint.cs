// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.ThreadDump;

public class ThreadDumpEndpoint : IEndpoint<List<ThreadInfo>>, IThreadDumpEndpoint
{
    private readonly IOptionsMonitor<ThreadDumpEndpointOptions> _options;
    private readonly IThreadDumper _threadDumper;

   // public new IThreadDumpOptions Options => options as IThreadDumpOptions;

    public ThreadDumpEndpoint(IOptionsMonitor<ThreadDumpEndpointOptions> options, IThreadDumper threadDumper, ILogger<ThreadDumpEndpoint> logger = null)
       // : base(options)
    {
        ArgumentGuard.NotNull(threadDumper);
        _options = options;
        _threadDumper = threadDumper;
    }

    public IOptionsMonitor<ThreadDumpEndpointOptions> Options => _options;

    IEndpointOptions IEndpoint.Options => _options.CurrentValue;

    public  List<ThreadInfo> Invoke()
    {
        return _threadDumper.DumpThreads();
    }
}
