// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.ThreadDump;

internal sealed class ThreadDumpEndpointV2 : IThreadDumpEndpointV2
{
    private readonly IOptionsMonitor<ThreadDumpEndpointOptions> _options;
    private readonly IThreadDumper _threadDumper;
    private readonly ILogger<ThreadDumpEndpointV2> _logger;

    public IHttpMiddlewareOptions Options => _options.CurrentValue;

    public ThreadDumpEndpointV2(IOptionsMonitor<ThreadDumpEndpointOptions> options, IThreadDumper threadDumper, ILogger<ThreadDumpEndpointV2> logger)
    {
        ArgumentGuard.NotNull(threadDumper);
        ArgumentGuard.NotNull(logger);
        _options = options;
        _threadDumper = threadDumper;
        _logger = logger;
    }

    public Task<ThreadDumpResult> InvokeAsync(CancellationToken cancellationToken)
    {
        _logger.LogTrace("Dumping Thread info");

        return Task.Run(() => new ThreadDumpResult
        {
            Threads = _threadDumper.DumpThreads()
        }, cancellationToken);
    }
}
