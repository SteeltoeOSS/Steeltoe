// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.HeapDump;

internal sealed class HeapDumpEndpointHandler : IHeapDumpEndpointHandler
{
    private readonly IHeapDumper _heapDumper;
    private readonly ILogger<HeapDumpEndpointHandler> _logger;
    private readonly IOptionsMonitor<HeapDumpEndpointOptions> _optionsMonitor;

    public HttpMiddlewareOptions Options => _optionsMonitor.CurrentValue;

    public HeapDumpEndpointHandler(IOptionsMonitor<HeapDumpEndpointOptions> optionsMonitor, IHeapDumper heapDumper, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(optionsMonitor);
        ArgumentGuard.NotNull(heapDumper);
        ArgumentGuard.NotNull(loggerFactory);
        _optionsMonitor = optionsMonitor;
        _heapDumper = heapDumper;
        _logger = loggerFactory.CreateLogger<HeapDumpEndpointHandler>();
    }

    public Task<string> InvokeAsync(object argument, CancellationToken cancellationToken)
    {
        _logger.LogTrace("Invoking the heap dumper");
        return Task.Run(_heapDumper.DumpHeapToFile, cancellationToken);
    }
}
