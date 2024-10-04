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
using Steeltoe.Common.Extensions;

namespace Steeltoe.Management.Endpoint.Actuators.ThreadDump;

/// <summary>
/// Thread dumper that uses the EventPipe to acquire the call stacks of all the running threads.
/// </summary>
public sealed class EventPipeThreadDumper
{
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
        List<ThreadInfo> results = [];

        try
        {
            _logger.LogDebug("Starting thread dump");

            var client = new DiagnosticsClient(System.Environment.ProcessId);
            List<EventPipeProvider> providers = [new("Microsoft-DotNETCore-SampleProfiler", EventLevel.Informational)];

            using EventPipeSession session = client.StartEventPipeSession(providers);
            await DumpThreadsAsync(session, results, cancellationToken);
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            _logger.LogError(exception, "Unable to dump threads");
        }
        finally
        {
            long totalMemory = GC.GetTotalMemory(true);
            _logger.LogDebug("Total Memory {Memory}", totalMemory);
        }

        return results;
    }

    // Much of this code is from diagnostics/dotnet-stack
    private async Task DumpThreadsAsync(EventPipeSession session, List<ThreadInfo> results, CancellationToken cancellationToken)
    {
        string? traceFileName = null;

        try
        {
            traceFileName = await CreateTraceFileAsync(session, cancellationToken);

            if (!string.IsNullOrEmpty(traceFileName))
            {
                using var symbolReader = new SymbolReader(TextWriter.Null);
                symbolReader.SymbolPath = System.Environment.CurrentDirectory;

                using var eventLog = new TraceLog(traceFileName);

                var stackSource = new MutableTraceEventStackSource(eventLog)
                {
                    OnlyManagedCodeStacks = true
                };

                var computer = new SampleProfilerThreadTimeComputer(eventLog, symbolReader);
                computer.GenerateThreadTimeStacks(stackSource);

                var samplesForThread = new Dictionary<int, List<StackSourceSample>>();

                stackSource.ForEach(sample =>
                {
                    StackSourceCallStackIndex stackIndex = sample.StackIndex;

                    while (!stackSource.GetFrameName(stackSource.GetFrameIndex(stackIndex), false).StartsWith("Thread (", StringComparison.Ordinal))
                    {
                        stackIndex = stackSource.GetCallerIndex(stackIndex);
                    }

                    const string template = "Thread (";
                    string threadFrame = stackSource.GetFrameName(stackSource.GetFrameIndex(stackIndex), false);
                    int threadId = int.Parse(threadFrame.AsSpan(template.Length, threadFrame.Length - (template.Length + 1)), CultureInfo.InvariantCulture);

                    if (samplesForThread.TryGetValue(threadId, out List<StackSourceSample>? samples))
                    {
                        samples.Add(sample);
                    }
                    else
                    {
                        samplesForThread[threadId] = [sample];
                    }
                });

                foreach ((int threadId, List<StackSourceSample> samples) in samplesForThread)
                {
                    _logger.LogDebug("Found {Stacks} stacks for thread {Thread}", samples.Count, threadId);

                    var threadInfo = new ThreadInfo
                    {
                        ThreadId = threadId,
                        ThreadName = $"Thread-{threadId}",
                        ThreadState = State.Runnable,
                        IsInNative = false,
                        IsSuspended = false
                    };

                    List<StackTraceElement> stackTrace = GetStackTrace(threadId, samples[0], stackSource, symbolReader);

                    foreach (StackTraceElement stackFrame in stackTrace)
                    {
                        threadInfo.StackTrace.Add(stackFrame);
                    }

                    threadInfo.ThreadState = GetThreadState(threadInfo.StackTrace);
                    threadInfo.IsInNative = IsThreadInNative(threadInfo.StackTrace);
                    results.Add(threadInfo);
                }
            }
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            _logger.LogError(exception, "Error processing trace file for thread dump");
            results.Clear();
        }
        finally
        {
            if (!string.IsNullOrEmpty(traceFileName) && File.Exists(traceFileName))
            {
                File.Delete(traceFileName);
            }
        }

        _logger.LogTrace("Finished thread walk");
    }

    private bool IsThreadInNative(IList<StackTraceElement> frames)
    {
        if (frames.Count > 0)
        {
            return frames[0] == NativeStackTraceElement;
        }

        return false;
    }

    private State GetThreadState(IList<StackTraceElement> frames)
    {
        if (IsThreadInNative(frames) && frames.Count > 1 && frames[1].MethodName!.Contains("Wait", StringComparison.OrdinalIgnoreCase))
        {
            return State.Waiting;
        }

        return State.Runnable;
    }

    private List<StackTraceElement> GetStackTrace(int threadId, StackSourceSample stackSourceSample, TraceEventStackSource stackSource,
        SymbolReader symbolReader)
    {
        _logger.LogDebug("Processing thread with ID: {Thread}", threadId);

        List<StackTraceElement> result = [];
        StackSourceCallStackIndex stackIndex = stackSourceSample.StackIndex;

        while (!stackSource.GetFrameName(stackSource.GetFrameIndex(stackIndex), false).StartsWith("Thread (", StringComparison.Ordinal))
        {
            StackSourceFrameIndex frameIndex = stackSource.GetFrameIndex(stackIndex);
            string frameName = stackSource.GetFrameName(frameIndex, false);
            SourceLocation? sourceLine = GetSourceLine(stackSource, frameIndex, symbolReader);
            StackTraceElement stackElement = GetStackTraceElement(frameName, sourceLine);
            result.Add(stackElement);

            stackIndex = stackSource.GetCallerIndex(stackIndex);
        }

        return result;
    }

    private StackTraceElement GetStackTraceElement(string frameName, SourceLocation? sourceLocation)
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

        if (TryParseFrameName(frameName, out string? assemblyName, out string? className, out string? methodName, out string? parameters))
        {
            result.ClassName = $"{assemblyName}!{className}";
            result.MethodName = $"{methodName}{parameters}";
        }

        if (sourceLocation != null)
        {
            result.LineNumber = sourceLocation.LineNumber;

            if (sourceLocation.SourceFile != null)
            {
                result.FileName = sourceLocation.SourceFile.BuildTimeFilePath;
            }
        }

        return result;
    }

    private bool TryParseFrameName(string frameName, [NotNullWhen(true)] out string? assemblyName, [NotNullWhen(true)] out string? className,
        [NotNullWhen(true)] out string? methodName, [NotNullWhen(true)] out string? parameters)
    {
        assemblyName = null;
        className = null;
        methodName = null;
        parameters = null;

        if (string.IsNullOrEmpty(frameName))
        {
            return false;
        }

        string remaining = frameName;

        if (!TryParseParameters(remaining, ref remaining, out parameters))
        {
            return false;
        }

        if (!TryParseMethod(remaining, ref remaining, out methodName))
        {
            return false;
        }

        ParseClassName(remaining, out remaining, out className);

        int dotIndex = remaining.IndexOf('.');
        assemblyName = dotIndex > 0 ? remaining[..dotIndex] : remaining;

        return true;
    }

    private void ParseClassName(string input, out string remaining, out string className)
    {
        int classStartIndex = input.IndexOf('!');

        if (classStartIndex == -1)
        {
            className = input;
            remaining = string.Empty;
        }
        else
        {
            remaining = input[..classStartIndex];
            className = input[(classStartIndex + 1)..];
        }
    }

    private bool TryParseMethod(string input, ref string remaining, [NotNullWhen(true)] out string? methodName)
    {
        int methodStartIndex = input.LastIndexOf('.');

        if (methodStartIndex > 0)
        {
            remaining = input[..methodStartIndex];
            methodName = input[(methodStartIndex + 1)..];
            return true;
        }

        methodName = null;
        return false;
    }

    private bool TryParseParameters(string input, ref string remaining, [NotNullWhen(true)] out string? parameters)
    {
        int paramStartIndex = input.IndexOf('(');

        if (paramStartIndex > 0)
        {
            remaining = input[..paramStartIndex];
            parameters = input[paramStartIndex..];
            return true;
        }

        parameters = null;
        return false;
    }

    // Much of this code is from PerfView/TraceLog.cs
    private SourceLocation? GetSourceLine(TraceEventStackSource stackSource, StackSourceFrameIndex frameIndex, SymbolReader reader)
    {
        TraceLog log = stackSource.TraceLog;
        uint codeAddress = (uint)frameIndex - (uint)StackSourceFrameIndex.Start;

        if (codeAddress >= log.CodeAddresses.Count)
        {
            return null;
        }

        var codeAddressIndex = (CodeAddressIndex)codeAddress;
        TraceModuleFile moduleFile = log.CodeAddresses.ModuleFile(codeAddressIndex);

        if (moduleFile == null)
        {
            string hexAddress = log.CodeAddresses.Address(codeAddressIndex).ToString("X", CultureInfo.InvariantCulture);
            _logger.LogTrace("GetSourceLine: Could not find moduleFile {HexAddress}.", hexAddress);
            return null;
        }

        MethodIndex methodIndex = log.CodeAddresses.MethodIndex(codeAddressIndex);

        if (methodIndex == MethodIndex.Invalid)
        {
            string hexAddress = log.CodeAddresses.Address(codeAddressIndex).ToString("X", CultureInfo.InvariantCulture);
            _logger.LogTrace("GetSourceLine: Could not find method for {HexAddress}", hexAddress);
            return null;
        }

        int methodToken = log.CodeAddresses.Methods.MethodToken(methodIndex);

        if (methodToken == 0)
        {
            string hexAddress = log.CodeAddresses.Address(codeAddressIndex).ToString("X", CultureInfo.InvariantCulture);
            _logger.LogTrace("GetSourceLine: Could not find method for {HexAddress}", hexAddress);
            return null;
        }

        int ilOffset = log.CodeAddresses.ILOffset(codeAddressIndex);

        if (ilOffset < 0)
        {
            ilOffset = 0;
        }

        string? pdbFileName = null;

        if (moduleFile.PdbSignature != Guid.Empty)
        {
            pdbFileName = reader.FindSymbolFilePath(moduleFile.PdbName, moduleFile.PdbSignature, moduleFile.PdbAge, moduleFile.FilePath,
                moduleFile.ProductVersion, true);
        }

        if (pdbFileName == null)
        {
            string simpleName = Path.GetFileNameWithoutExtension(moduleFile.FilePath);

            if (simpleName.EndsWith(".il", StringComparison.Ordinal))
            {
                simpleName = Path.GetFileNameWithoutExtension(simpleName);
            }

            pdbFileName = reader.FindSymbolFilePath($"{simpleName}.pdb", Guid.Empty, 0);
        }

        if (pdbFileName != null)
        {
            ManagedSymbolModule symbolReaderModule = reader.OpenSymbolFile(pdbFileName);

            if (symbolReaderModule != null)
            {
                if (moduleFile.PdbSignature != Guid.Empty && symbolReaderModule.PdbGuid != moduleFile.PdbSignature)
                {
                    _logger.LogTrace("ERROR: the PDB we opened does not match the PDB desired. PDB GUID = {PdbGuid}, DESIRED GUID = {DesiredGuid}",
                        symbolReaderModule.PdbGuid, moduleFile.PdbSignature);

                    return null;
                }

                symbolReaderModule.ExePath = moduleFile.FilePath;
                return symbolReaderModule.SourceLocationForManagedCode((uint)methodToken, ilOffset);
            }
        }

        return null;
    }

    private async Task<string> CreateTraceFileAsync(EventPipeSession session, CancellationToken cancellationToken)
    {
        string tempNetTraceFilename = $"{Path.GetRandomFileName()}.nettrace";

        try
        {
            await using (FileStream outputStream = File.OpenWrite(tempNetTraceFilename))
            {
                Task copyTask = session.EventStream.CopyToAsync(outputStream, cancellationToken);
                await Task.Delay(_optionsMonitor.CurrentValue.Duration, cancellationToken);
                await session.StopAsync(cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                // check if rundown is taking more than 5 seconds and log
                Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                Task completedTask = await Task.WhenAny(copyTask, timeoutTask);

                if (completedTask == timeoutTask && !cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Sufficiently large applications can cause this command to take non-trivial amounts of time");
                }

                await copyTask;
            }

            // using the generated trace file, symbolocate and compute stacks.
            return TraceLog.CreateFromEventPipeDataFile(tempNetTraceFilename);
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            _logger.LogError(exception, "Error creating trace file for thread dump");
            return string.Empty;
        }
        finally
        {
            if (File.Exists(tempNetTraceFilename))
            {
                File.Delete(tempNetTraceFilename);
            }
        }
    }
}
