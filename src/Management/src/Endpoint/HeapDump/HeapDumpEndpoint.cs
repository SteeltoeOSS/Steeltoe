// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.HeapDump;

internal sealed class HeapDumpEndpoint : IHeapDumpEndpoint
{
    private readonly IHeapDumper _heapDumper;
    private readonly ILogger<HeapDumpEndpoint> _logger;

   
    public IOptionsMonitor<HeapDumpEndpointOptions> Options { get; }

    public HeapDumpEndpoint(IOptionsMonitor<HeapDumpEndpointOptions> options, IHeapDumper heapDumper, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(heapDumper);
        ArgumentGuard.NotNull(loggerFactory);

        Options = options;
        _heapDumper = heapDumper;
        _logger = loggerFactory.CreateLogger<HeapDumpEndpoint>();
    }

    public Task<string> InvokeAsync(CancellationToken cancellationToken)
    {
        _logger.LogTrace("Invoking the heap dumper");
        return Task.Run(() => _heapDumper.DumpHeap(), cancellationToken);
    }
}
