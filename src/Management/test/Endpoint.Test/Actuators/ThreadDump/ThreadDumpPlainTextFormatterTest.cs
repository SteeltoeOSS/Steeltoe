// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.ThreadDump;

namespace Steeltoe.Management.Endpoint.Test.Actuators.ThreadDump;

public sealed class ThreadDumpPlainTextFormatterTest
{
    [Fact]
    public void Formats_empty_thread_dump()
    {
        string text = ThreadDumpPlainTextFormatter.Format([]);

        text.Should().BeEmpty();
    }

    [Fact]
    public void Formats_threads_with_stack_frames()
    {
        IList<ThreadInfo> threads =
        [
            new()
            {
                ThreadId = 132,
                ThreadName = "Thread-00132",
                ThreadState = State.Waiting,
                IsInNative = true,
                StackTrace =
                {
                    new StackTraceElement
                    {
                        ClassName = "[NativeClasses]",
                        MethodName = "[NativeMethods]",
                        IsNativeMethod = true
                    },
                    new StackTraceElement
                    {
                        ModuleName = "System.Private.CoreLib",
                        ClassName = "System.Threading.WaitHandle",
                        MethodName = "WaitOneNoCheck(int32,bool,class System.Object,value class WaitHandleWaitSourceMap)"
                    },
                    new StackTraceElement
                    {
                        ModuleName = "ExampleApp",
                        ClassName = "ExampleApp.Worker",
                        MethodName = "Run()",
                        FileName = "/app/src/Worker.cs",
                        LineNumber = 42,
                        ColumnNumber = 13
                    },
                    new StackTraceElement
                    {
                        ModuleName = "ExampleApp",
                        ClassName = "ExampleApp.Program",
                        MethodName = "Main()",
                        FileName = "/app/src/Program.cs"
                    }
                }
            },
            new()
            {
                ThreadId = 7,
                ThreadName = "Thread-00007",
                ThreadState = State.TimedWaiting
            }
        ];

        string text = ThreadDumpPlainTextFormatter.Format(threads);

        text.Should().Be("""
            "Thread-00132" #132
               Thread State: WAITING
              at [NativeClasses].[NativeMethods](Unknown Source)
              at System.Threading.WaitHandle.WaitOneNoCheck(int32,bool,class System.Object,value class WaitHandleWaitSourceMap)(Unknown Source)
              at ExampleApp.Worker.Run()(/app/src/Worker.cs:42)
              at ExampleApp.Program.Main()(/app/src/Program.cs)

              Locked ownable synchronizers:
              - None

            "Thread-00007" #7
               Thread State: TIMED_WAITING

              Locked ownable synchronizers:
              - None


            """.ReplaceLineEndings("\n"));
    }
}
