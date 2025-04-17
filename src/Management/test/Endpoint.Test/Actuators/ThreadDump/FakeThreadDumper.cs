// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.ThreadDump;

namespace Steeltoe.Management.Endpoint.Test.Actuators.ThreadDump;

internal sealed class FakeThreadDumper : IThreadDumper
{
    public Task<IList<ThreadInfo>> DumpThreadsAsync(CancellationToken cancellationToken)
    {
        IList<ThreadInfo> threadInfos =
        [
            new()
            {
                StackTrace =
                {
                    new StackTraceElement
                    {
                        ClassName = "Test.FakeClass",
                        FileName = "FakeClass.cs",
                        LineNumber = 10,
                        MethodName = "FakeMethod"
                    }
                }
            }
        ];

        return Task.FromResult(threadInfos);
    }
}
