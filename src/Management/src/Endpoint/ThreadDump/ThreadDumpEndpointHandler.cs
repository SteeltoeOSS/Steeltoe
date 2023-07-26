// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.ThreadDump;

internal sealed class ThreadDumpEndpointHandler : IThreadDumpEndpointHandler
{
    private readonly IOptionsMonitor<ThreadDumpEndpointOptions> _options;
    private readonly ILogger<ThreadDumpEndpointHandler> _logger;
    private readonly EventPipeThreadDumper _threadDumper;

    public HttpMiddlewareOptions Options => _options.CurrentValue;

    public ThreadDumpEndpointHandler(IOptionsMonitor<ThreadDumpEndpointOptions> options, EventPipeThreadDumper threadDumper, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(threadDumper);
        ArgumentGuard.NotNull(loggerFactory);

        _options = options;
        _threadDumper = threadDumper;
        _logger = loggerFactory.CreateLogger<ThreadDumpEndpointHandler>();
    }

    public async Task<IList<ThreadInfo>> InvokeAsync(object argument, CancellationToken cancellationToken)
    {
        _logger.LogTrace("Invoking ThreadDumper");
        return await _threadDumper.DumpThreadsAsync(cancellationToken);
    }
}
