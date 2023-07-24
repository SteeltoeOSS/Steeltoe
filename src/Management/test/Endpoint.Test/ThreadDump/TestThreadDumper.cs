// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.ThreadDump;

namespace Steeltoe.Management.Endpoint.Test.ThreadDump;

internal sealed class TestThreadDumper : IThreadDumper
{
    public bool DumpThreadsCalled { get; private set; }

    public IList<ThreadInfo> DumpThreads()
    {
        DumpThreadsCalled = true;
        return new List<ThreadInfo>();
    }
}
