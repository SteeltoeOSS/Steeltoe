// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.HeapDump;

public class HeapDumpEndpoint : AbstractEndpoint<string>, IHeapDumpEndpoint
{
    private readonly IHeapDumper _heapDumper;

    public new IHeapDumpOptions Options => options as IHeapDumpOptions;

    public HeapDumpEndpoint(IHeapDumpOptions options, IHeapDumper heapDumper, ILogger<HeapDumpEndpoint> logger = null)
        : base(options)
    {
        ArgumentGuard.NotNull(heapDumper);

        _heapDumper = heapDumper;
    }

    public override string Invoke()
    {
        return _heapDumper.DumpHeap();
    }
}
