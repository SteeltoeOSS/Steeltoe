// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.ThreadDump;

public class ThreadDumpEndpoint : AbstractEndpoint<List<ThreadInfo>>, IThreadDumpEndpoint
{
    private readonly ILogger<ThreadDumpEndpoint> _logger;
    private readonly IThreadDumper _threadDumper;

    public ThreadDumpEndpoint(IThreadDumpOptions options, IThreadDumper threadDumper, ILogger<ThreadDumpEndpoint> logger = null)
        : base(options)
    {
        _threadDumper = threadDumper ?? throw new ArgumentNullException(nameof(threadDumper));
        _logger = logger;
    }

    public new IThreadDumpOptions Options
    {
        get
        {
            return options as IThreadDumpOptions;
        }
    }

    public override List<ThreadInfo> Invoke()
    {
        return _threadDumper.DumpThreads();
    }
}
