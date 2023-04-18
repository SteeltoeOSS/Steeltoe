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
    private readonly ILogger<ThreadDumpEndpoint> _logger;
    private readonly IThreadDumper _threadDumper;

    public IEndpointOptions Options => _options.CurrentValue;

    public ThreadDumpEndpoint(IOptionsMonitor<ThreadDumpEndpointOptions> options, IThreadDumper threadDumper, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(threadDumper);
        ArgumentGuard.NotNull(loggerFactory);

        _options = options;
        _threadDumper = threadDumper;
        _logger = loggerFactory.CreateLogger<ThreadDumpEndpoint>();
    }

    public virtual IList<ThreadInfo> Invoke()
    {
        _logger.LogTrace("Invoking ThreadDumper");
        return _threadDumper.DumpThreads();
    }
}
