// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Globalization;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Symbols;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Stacks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Steeltoe.Management.Endpoint.Actuators.ThreadDump;

/// <summary>
/// Thread dumper that uses the EventPipe to acquire the call stacks of all the running threads.
/// </summary>
internal sealed class EventPipeThreadDumper : IThreadDumper
{
    private const string ThreadIdTemplate = "Thread (";

    private static readonly StackTraceElement UnknownStackTraceElement = new()
    {
        ClassName = "[UnknownClass]",
        MethodName = "[UnknownMethod]",
        IsNativeMethod = true
    };

    private static readonly StackTraceElement NativeStackTraceElement = new()
    {
        ClassName = "[NativeClasses]",
        MethodName = "[NativeMethods]",
        IsNativeMethod = true
    };

    private readonly IOptionsMonitor<ThreadDumpEndpointOptions> _optionsMonitor;
    private readonly ILogger<EventPipeThreadDumper> _logger;

    public EventPipeThreadDumper(IOptionsMonitor<ThreadDumpEndpointOptions> optionsMonitor, ILogger<EventPipeThreadDumper> logger)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(logger);

        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    /// <summary>
    /// Connect using the EventPipe and obtain a dump of all the threads, and for each thread a stack trace.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// The list of threads with stack trace information.
    /// </returns>
    public async Task<IList<ThreadInfo>> DumpThreadsAsync(CancellationToken cancellationToken)
    {
        return await CaptureLogOutputAsync(async logWriter =>
        {
            try
            {
                _logger.LogInformation("Attempting to create a thread dump.");

                var client = new DiagnosticsClient(System.Environment.ProcessId);
                List<EventPipeProvider> providers = [new("Microsoft-DotNETCore-SampleProfiler", EventLevel.Informational)];

                // Based on the code at https://github.com/dotnet/diagnostics/tree/v9.0.621003/src/Tools/dotnet-stack.
                using EventPipeSession session = client.StartEventPipeSession(providers);
                List<ThreadInfo> threads = await GetThreadsFromEventPipeSessionAsync(session, logWriter, cancellationToken);

                _logger.LogInformation("Successfully created a thread dump.");
                return threads;
            }
            finally
            {
                long totalMemory = GC.GetTotalMemory(true);
                _logger.LogDebug("Total memory: {Memory}.", totalMemory);
            }
        }, cancellationToken);
    }

    private async Task<TResult> CaptureLogOutputAsync<TResult>(Func<TextWriter, Task<TResult>> action, CancellationToken cancellationToken)
    {
        bool isLogEnabled = _logger.IsEnabled(LogLevel.Trace);
        using var logStream = new MemoryStream();
        Exception? error = null;
        TResult? result = default;

        await using (TextWriter logWriter = isLogEnabled ? new StreamWriter(logStream, leaveOpen: true) : TextWriter.Null)
        {
            try
            {
                result = await action(logWriter);
                await logWriter.FlushAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                error = exception;
            }
        }

        string? logOutput = null;

        if (isLogEnabled)
        {
            logStream.Seek(0, SeekOrigin.Begin);
            using var logReader = new StreamReader(logStream);
            logOutput = await logReader.ReadToEndAsync(cancellationToken);
        }

        if (error != null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string message = isLogEnabled
                ? $"Failed to create a thread dump. Captured log:{System.Environment.NewLine}{logOutput}"
                : "Failed to create a thread dump.";

            throw new InvalidOperationException(message, error);
        }

        if (isLogEnabled)
        {
            _logger.LogTrace("Captured log from thread dump:{LineBreak}{DumpLog}", System.Environment.NewLine, logOutput);
        }

        return result!;
    }

    private async Task<List<ThreadInfo>> GetThreadsFromEventPipeSessionAsync(EventPipeSession session, TextWriter logWriter,
        CancellationToken cancellationToken)
    {
        string? traceFileName = null;

        try
        {
            traceFileName = await CreateTraceFileAsync(session, cancellationToken);
            using SymbolReader symbolReader = CreateSymbolReader(logWriter);
            using var eventLog = new TraceLog(traceFileName);

            var stackSource = new MutableTraceEventStackSource(eventLog)
            {
                OnlyManagedCodeStacks = true
            };

            var computer = new SampleProfilerThreadTimeComputer(eventLog, symbolReader);
            computer.GenerateThreadTimeStacks(stackSource);

            List<ThreadInfo> results = ReadStackSource(stackSource, symbolReader).ToList();

            _logger.LogTrace("Finished thread walk.");
            return results;
        }
        finally
        {
            SafeDelete(traceFileName);
        }
    }

    private async Task<string> CreateTraceFileAsync(EventPipeSession session, CancellationToken cancellationToken)
    {
        string tempNetTraceFilename = Path.Join(Path.GetTempPath(), Path.GetRandomFileName() + ".nettrace");

        try
        {
            await using (FileStream outputStream = File.OpenWrite(tempNetTraceFilename))
            {
                Task copyTask = session.EventStream.CopyToAsync(outputStream, cancellationToken);
                await Task.Delay(_optionsMonitor.CurrentValue.Duration, cancellationToken);
                await session.StopAsync(cancellationToken);

                // Check if rundown is taking more than 5 seconds and log.
                try
                {
                    await copyTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
                }
                catch (TimeoutException) when (!cancellationToken.IsCancellationRequested)
                {
#pragma warning disable S6667 // Logging in a catch clause should pass the caught exception as a parameter.
                    _logger.LogInformation("Sufficiently large applications can cause this command to take non-trivial amounts of time.");
#pragma warning restore S6667 // Logging in a catch clause should pass the caught exception as a parameter.

                    throw;
                }
            }

            // Using the generated trace file, symbolicate and compute stacks.
            return TraceLog.CreateFromEventPipeDataFile(tempNetTraceFilename);
        }
        finally
        {
            SafeDelete(tempNetTraceFilename);
        }
    }

    private static void SafeDelete(string? outputPath)
    {
        if (outputPath != null)
        {
            try
            {
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
            }
            catch (Exception)
            {
                // Intentionally left empty.
            }
        }
    }

    private static SymbolReader CreateSymbolReader(TextWriter logWriter)
    {
        return new SymbolReader(logWriter)
        {
            SymbolPath = System.Environment.CurrentDirectory,

            // The next line prevents the following message in captured logs:
            //   Found PDB <path>.pdb, however this is in an unsafe location.
            //   If you trust this location, place this directory the symbol path to correct this (or use the SecurityCheck method to override)
            SecurityCheck = pdbPath => pdbPath.StartsWith(System.Environment.CurrentDirectory, StringComparison.OrdinalIgnoreCase)
        };
    }

    private IEnumerable<ThreadInfo> ReadStackSource(MutableTraceEventStackSource stackSource, SymbolReader symbolReader)
    {
        var samplesForThread = new Dictionary<int, List<StackSourceSample>>();

        stackSource.ForEach(sample =>
        {
            StackSourceCallStackIndex stackIndex = sample.StackIndex;

            while (!stackSource.GetFrameName(stackSource.GetFrameIndex(stackIndex), false).StartsWith(ThreadIdTemplate, StringComparison.Ordinal))
            {
                stackIndex = stackSource.GetCallerIndex(stackIndex);
            }

            string frameName = stackSource.GetFrameName(stackSource.GetFrameIndex(stackIndex), false);
            int threadId = ExtractThreadId(frameName);

            if (samplesForThread.TryGetValue(threadId, out List<StackSourceSample>? samples))
            {
                samples.Add(sample);
            }
            else
            {
                samplesForThread[threadId] = [sample];
            }
        });

        // For every thread recorded in our trace, use the first stack.
        foreach ((int threadId, List<StackSourceSample> samples) in samplesForThread)
        {
            _logger.LogDebug("Found {Stacks} stacks for thread {Thread}.", samples.Count, threadId);

            var threadInfo = new ThreadInfo
            {
                ThreadId = threadId,
                // Not available in .NET, but Apps Manager crashes without it.
                ThreadName = $"Thread-{threadId:D5}"
            };

            List<StackTraceElement> stackTrace = GetStackTrace(threadId, samples[0], stackSource, symbolReader).ToList();

            foreach (StackTraceElement stackFrame in stackTrace)
            {
                threadInfo.StackTrace.Add(stackFrame);
            }

            SetThreadState(threadInfo);
            yield return threadInfo;
        }
    }

    private static int ExtractThreadId(string frameName)
    {
        // Handle a thread name like: Thread (4008) (.NET IO ThreadPool Worker)

        int firstIndex = frameName.IndexOf(')');
        return int.Parse(frameName.AsSpan(ThreadIdTemplate.Length, firstIndex - ThreadIdTemplate.Length), CultureInfo.InvariantCulture);
    }

    private IEnumerable<StackTraceElement> GetStackTrace(int threadId, StackSourceSample stackSourceSample, TraceEventStackSource stackSource,
        SymbolReader symbolReader)
    {
        _logger.LogDebug("Processing thread with ID: {Thread}.", threadId);

        StackSourceCallStackIndex stackIndex = stackSourceSample.StackIndex;
        StackSourceFrameIndex frameIndex = stackSource.GetFrameIndex(stackIndex);
        string frameName = stackSource.GetFrameName(frameIndex, false);

        while (!frameName.StartsWith(ThreadIdTemplate, StringComparison.Ordinal))
        {
            SourceLocation? sourceLocation = stackSource.GetSourceLine(frameIndex, symbolReader);
            StackTraceElement stackElement = GetStackTraceElement(frameName, sourceLocation);

            yield return stackElement;

            stackIndex = stackSource.GetCallerIndex(stackIndex);
            frameIndex = stackSource.GetFrameIndex(stackIndex);
            frameName = stackSource.GetFrameName(frameIndex, false);
        }
    }

    private static StackTraceElement GetStackTraceElement(string frameName, SourceLocation? sourceLocation)
    {
        if (string.IsNullOrEmpty(frameName))
        {
            return UnknownStackTraceElement;
        }

        if (frameName.Contains("UNMANAGED_CODE_TIME", StringComparison.OrdinalIgnoreCase) || frameName.Contains("CPU_TIME", StringComparison.OrdinalIgnoreCase))
        {
            return NativeStackTraceElement;
        }

        var result = new StackTraceElement
        {
            MethodName = "Unknown",
            IsNativeMethod = false,
            ClassName = "Unknown"
        };

        if (StackFrameSymbol.TryParse(frameName, out StackFrameSymbol? symbol))
        {
            result.ModuleName = symbol.AssemblyName;
            result.ClassName = symbol.TypeName;
            result.MethodName = $"{symbol.MemberName}{symbol.Parameters}";
        }

        result.FileName = sourceLocation?.SourceFile?.BuildTimeFilePath;
        result.LineNumber = sourceLocation?.LineNumber;
        result.ColumnNumber = sourceLocation?.ColumnNumber;

        return result;
    }

    private static void SetThreadState(ThreadInfo threadInfo)
    {
        threadInfo.IsInNative = threadInfo.StackTrace.Count > 0 && threadInfo.StackTrace[0] == NativeStackTraceElement;

        threadInfo.ThreadState =
            threadInfo is { IsInNative: true, StackTrace.Count: > 1 } &&
            threadInfo.StackTrace[1].MethodName?.Contains("Wait", StringComparison.OrdinalIgnoreCase) == true
                ? State.Waiting
                : State.Runnable;
    }

    private sealed record StackFrameSymbol(string AssemblyName, string TypeName, string MemberName, string Parameters)
    {
        public static bool TryParse(string frameName, [NotNullWhen(true)] out StackFrameSymbol? symbol)
        {
            symbol = null;

            if (string.IsNullOrEmpty(frameName))
            {
                return false;
            }

            ReadOnlySpan<char> remaining = frameName;

            if (!TryEatParameters(ref remaining, out string? parameters))
            {
                return false;
            }

            if (!TryEatMemberName(ref remaining, out string? memberName))
            {
                return false;
            }

            if (!TryEatTypeName(ref remaining, out string? typeName))
            {
                return false;
            }

            string assemblyName = remaining.ToString();

            symbol = new StackFrameSymbol(assemblyName, typeName, memberName, parameters);
            return true;
        }

        private static bool TryEatParameters(ref ReadOnlySpan<char> remaining, [NotNullWhen(true)] out string? parameters)
        {
            int startIndex = remaining.IndexOf('(');

            if (startIndex < 0)
            {
                parameters = null;
                return false;
            }

            parameters = remaining[startIndex..].ToString();
            remaining = remaining[..startIndex];
            return true;
        }

        private static bool TryEatMemberName(ref ReadOnlySpan<char> remaining, [NotNullWhen(true)] out string? memberName)
        {
            int startIndex = remaining.LastIndexOf('.');

            if (startIndex < 0)
            {
                memberName = null;
                return false;
            }

            memberName = remaining[(startIndex + 1)..].ToString();
            remaining = remaining[..startIndex];
            return true;
        }

        private static bool TryEatTypeName(ref ReadOnlySpan<char> remaining, [NotNullWhen(true)] out string? typeName)
        {
            int startIndex = remaining.IndexOf('!');

            if (startIndex < 0)
            {
                typeName = null;
                return false;
            }

            typeName = remaining[(startIndex + 1)..].ToString();
            remaining = remaining[..startIndex];
            return true;
        }
    }
}
