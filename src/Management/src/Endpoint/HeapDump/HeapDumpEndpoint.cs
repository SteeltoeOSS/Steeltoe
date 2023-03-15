// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.HeapDump;

public class HeapDumpEndpoint : IHeapDumpEndpoint
{
    private readonly IHeapDumper _heapDumper;

    IEndpointOptions IEndpoint.Options => Options.CurrentValue;

    public IOptionsMonitor<HeapDumpEndpointOptions> Options { get; }

    public HeapDumpEndpoint(IOptionsMonitor<HeapDumpEndpointOptions> options, IHeapDumper heapDumper, ILogger<HeapDumpEndpoint> logger = null)
    {
        ArgumentGuard.NotNull(heapDumper);
        Options = options;
        _heapDumper = heapDumper;
    }

    public string Invoke()
    {
        return _heapDumper.DumpHeap();
    }
}
