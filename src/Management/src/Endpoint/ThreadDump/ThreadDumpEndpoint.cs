// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.ThreadDump;

public class ThreadDumpEndpoint : IThreadDumpEndpoint
{
    private readonly IOptionsMonitor<ThreadDumpEndpointOptions> _options;
    private readonly IThreadDumper _threadDumper;

    public ThreadDumpEndpoint(IOptionsMonitor<ThreadDumpEndpointOptions> options, IThreadDumper threadDumper, ILogger<ThreadDumpEndpoint> logger = null)
    {
        ArgumentGuard.NotNull(threadDumper);
        _options = options;

        _threadDumper = threadDumper;
    }

    public IEndpointOptions Options => _options.CurrentValue;

    public List<ThreadInfo> Invoke()
    {
        return _threadDumper.DumpThreads();
    }
}