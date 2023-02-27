// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.HeapDump;

public class HeapDumpEndpoint : /*AbstractEndpoint<string>*/ IEndpoint<string>, IHeapDumpEndpoint
{
    private readonly IOptionsMonitor<HeapDumpEndpointOptions> _options;
    private readonly IHeapDumper _heapDumper;

  //  public new IHeapDumpOptions Options => options as IHeapDumpOptions;

    public HeapDumpEndpoint(IOptionsMonitor<HeapDumpEndpointOptions> options, IHeapDumper heapDumper, ILogger<HeapDumpEndpoint> logger = null)
       // : base(options)
    {
        ArgumentGuard.NotNull(heapDumper);
        _options = options;
        _heapDumper = heapDumper;
    }

    public IOptionsMonitor<HeapDumpEndpointOptions> Options => _options;

    IEndpointOptions IEndpoint.Options => _options.CurrentValue;

    public string Invoke()
    {
        return _heapDumper.DumpHeap();
    }
}
