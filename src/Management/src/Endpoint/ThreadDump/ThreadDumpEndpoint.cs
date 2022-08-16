// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.ThreadDump;

public class ThreadDumpEndpoint : AbstractEndpoint<IReadOnlyList<ThreadInfo>>, IThreadDumpEndpoint
{
    private readonly ILogger<ThreadDumpEndpoint> _logger;
    private readonly IThreadDumper _threadDumper;

    public new IThreadDumpOptions Options => options as IThreadDumpOptions;

    public ThreadDumpEndpoint(IThreadDumpOptions options, IThreadDumper threadDumper, ILogger<ThreadDumpEndpoint> logger = null)
        : base(options)
    {
        ArgumentGuard.NotNull(threadDumper);

        _threadDumper = threadDumper;
        _logger = logger;
    }

    public override IReadOnlyList<ThreadInfo> Invoke()
    {
        return _threadDumper.DumpThreads();
    }
}
