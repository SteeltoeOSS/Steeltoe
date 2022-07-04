// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;

namespace Steeltoe.Management.Endpoint.ThreadDump;

public class ThreadDumpEndpointV2 : AbstractEndpoint<ThreadDumpResult>, IThreadDumpEndpointV2
{
    private readonly ILogger<ThreadDumpEndpointV2> _logger;
    private readonly IThreadDumper _threadDumper;

    public ThreadDumpEndpointV2(IThreadDumpOptions options, IThreadDumper threadDumper, ILogger<ThreadDumpEndpointV2> logger = null)
        : base(options)
    {
        _threadDumper = threadDumper ?? throw new ArgumentNullException(nameof(threadDumper));
        _logger = logger;
    }

    public new IThreadDumpOptions Options
    {
        get
        {
            return innerOptions as IThreadDumpOptions;
        }
    }

    public override ThreadDumpResult Invoke()
    {
        return new ThreadDumpResult
        {
            Threads = _threadDumper.DumpThreads()
        };
    }
}
