// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;

namespace Steeltoe.Management.Endpoint.ThreadDump
{
    public class ThreadDumpEndpoint_v2 : AbstractEndpoint<ThreadDumpResult>
    {
        private readonly ILogger<ThreadDumpEndpoint_v2> _logger;
        private readonly IThreadDumper _threadDumper;

        public ThreadDumpEndpoint_v2(IThreadDumpOptions options, IThreadDumper threadDumper, ILogger<ThreadDumpEndpoint_v2> logger = null)
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

        public override ThreadDumpResult Invoke()
        {
            return new ThreadDumpResult
            {
                Threads = _threadDumper.DumpThreads()
            };
        }
    }
}
