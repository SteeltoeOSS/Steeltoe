// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.ThreadDump;

namespace Steeltoe.Management.Endpoint.Test.Actuators.ThreadDump;

internal sealed class FakeThreadDumper : IThreadDumper
{
    public Task<IList<ThreadInfo>> DumpThreadsAsync(CancellationToken cancellationToken)
    {
        IList<ThreadInfo> threads =
        [
            new()
            {
                ThreadId = 18,
                ThreadName = "Thread-00018",
                ThreadState = State.Waiting,
                StackTrace =
                {
                    new StackTraceElement
                    {
                        ModuleName = "FakeAssembly",
                        ClassName = "FakeNamespace.FakeClass",
                        FileName = @"C:\Source\Code\FakeClass.cs",
                        LineNumber = 8,
                        ColumnNumber = 16,
                        MethodName = "FakeMethod"
                    }
                }
            }
        ];

        return Task.FromResult(threads);
    }
}
