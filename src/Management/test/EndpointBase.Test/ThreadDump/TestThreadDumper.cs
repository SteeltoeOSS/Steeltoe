﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.ThreadDump.Test
{
    internal class TestThreadDumper : IThreadDumper
    {
        public bool DumpThreadsCalled { get; set; }

        public List<ThreadInfo> DumpThreads()
        {
            DumpThreadsCalled = true;
            return new List<ThreadInfo>();
        }
    }
}
