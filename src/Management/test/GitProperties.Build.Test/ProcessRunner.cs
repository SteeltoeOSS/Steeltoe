// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Steeltoe.Management.GitProperties.Build.Test;

internal static partial class ProcessRunner
{
    /// <summary>
    /// The runtime provider (keyword 0x8000, informational) records every exception thrown, including handled ones. The NuGet providers emit start/stop
    /// events for each phase of a restore (no-op calculation, restore graph, assets file, commit), which shows how far it got. The HTTP and DNS providers
    /// show feed requests and their outcome.
    /// </summary>
    private static readonly string EventPipeProviders = string.Concat((string[])
    [
        "Microsoft-Windows-DotNETRuntime:0x8000:4,",
        "Microsoft-NuGet-Commands:0xFFFFFFFFFFFFFFFF:5,",
        "Microsoft-NuGet-Common:0xFFFFFFFFFFFFFFFF:5,",
        "Microsoft-NuGet-Configuration:0xFFFFFFFFFFFFFFFF:5,",
        "Microsoft-System-Net-Http:0xFFFFFFFFFFFFFFFF:4,",
        "System.Net.NameResolution:0xFFFFFFFFFFFFFFFF:4"
    ]);

    private static readonly string LocatorCommand = OperatingSystem.IsWindows() ? "where" : "which";

    private static readonly char[] LineSeparators =
    [
        '\r',
        '\n'
    ];

    /// <summary>
    /// Generous enough to cover the slowest command this suite runs (a Release build plus NuGet pack) under heavy load, while still turning a genuine hang
    /// into an informative test failure instead of blocking the whole suite indefinitely.
    /// </summary>
    private static readonly TimeSpan ProcessExitTimeout = TimeSpan.FromMinutes(2);

    private static readonly string[] EnvironmentVariablePrefixes =
    [
        "DOTNET_",
        "CORECLR_",
        "COR_",
        "NUGET_",
        "MSBUILD",
        "TMP",
        "TEMP",
        "HOME"
    ];

    private static readonly string[] ProcessNamesToCapture =
    [
        "dotnet",
        "msbuild",
        "vbcscompiler",
        "testhost",
        "git"
    ];

    private static readonly Task<string> RealGitExecutableTask = ResolveGitExecutableAsync();
    private static readonly Task<string> DiagnosticsDirectoryTask = ResolveDiagnosticsDirectoryAsync();

    private static async Task<string> ResolveGitExecutableAsync()
    {
        string output = await RunAsync(LocatorCommand, Path.GetTempPath(), 0, null, CancellationToken.None, "git");
        string? firstLine = output.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();

        if (firstLine == null)
        {
            throw new InvalidOperationException($"Could not resolve the location of git via '{LocatorCommand} git'.");
        }

        return firstLine;
    }

    public static Task<string> RunGitAsync(string workingDirectory, params string[] arguments)
    {
        return RunGitAsync(workingDirectory, TestContext.Current.CancellationToken, arguments);
    }

    public static async Task<string> RunGitAsync(string workingDirectory, CancellationToken cancellationToken, params string[] arguments)
    {
        string gitExecutable = await RealGitExecutableTask;
        string output = await RunAsync(gitExecutable, workingDirectory, 0, null, cancellationToken, arguments);
        return output.Trim();
    }

    public static async Task RunDotNetBuildCapturingDiagnosticsOnFailureAsync(string workingDirectory, string diagnosticsFileNamePrefix,
        params string[] arguments)
    {
        string diagnosticsDirectory = await DiagnosticsDirectoryTask;
        string diagnosticsFilePrefix = Path.Combine(diagnosticsDirectory, $"{diagnosticsFileNamePrefix}-{$"{Guid.NewGuid():N}"[..8]}");
        string binlogPath = $"{diagnosticsFilePrefix}.binlog";

        // Records every exception thrown (including handled ones) in each spawned .NET process, which reveals failures that tasks swallow without logging
        // (such as RestoreTask returning false without an error). The runtime replaces {pid} with the process ID, so each process writes its own file.
        var traceEnvironmentVariables = new Dictionary<string, string>
        {
            ["DOTNET_EnableEventPipe"] = "1",
            ["DOTNET_EventPipeConfig"] = EventPipeProviders,
            ["DOTNET_EventPipeOutputPath"] = $"{diagnosticsFilePrefix}.{{pid}}.nettrace",
            ["DOTNET_EventPipeOutputStreaming"] = "1"
        };

        string[] argumentsWithBinlog =
        [
            "build",
            "-c",
            "Release",
            .. arguments,
            $"-bl:{binlogPath}"
        ];

        try
        {
            await RunDotNetAsync(workingDirectory, 0, traceEnvironmentVariables, argumentsWithBinlog);
        }
        catch (Exception)
        {
            await CaptureFailureContextAsync(workingDirectory, diagnosticsFilePrefix, traceEnvironmentVariables, arguments);
            throw;
        }

        DeleteDiagnosticsFiles(diagnosticsDirectory, Path.GetFileName(diagnosticsFilePrefix));
    }

    /// <summary>
    /// Best-effort capture of information that helps to determine whether a failed build is deterministic or transient and what the machine looked like.
    /// Never throws, so the original failure remains the one that is reported.
    /// </summary>
    private static async Task CaptureFailureContextAsync(string workingDirectory, string diagnosticsFilePrefix,
        Dictionary<string, string> traceEnvironmentVariables, string[] arguments)
    {
        var builder = new StringBuilder();

        try
        {
            builder.AppendLine($"Captured at: {DateTimeOffset.UtcNow:O}");
            builder.AppendLine($"OS: {RuntimeInformation.OSDescription}");
            builder.AppendLine($"Processor count: {Environment.ProcessorCount}");

            GCMemoryInfo memoryInfo = GC.GetGCMemoryInfo();
            builder.AppendLine($"Memory (available/load, bytes): {memoryInfo.TotalAvailableMemoryBytes}/{memoryInfo.MemoryLoadBytes}");

            foreach (string path in new[]
            {
                Path.GetTempPath(),
                workingDirectory,
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            }.Distinct())
            {
                var drive = new DriveInfo(path);
                builder.AppendLine($"Free disk space on '{drive.Name}' (for '{path}'): {drive.AvailableFreeSpace} bytes");
            }

            builder.AppendLine("Environment variables:");

            foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>().OrderBy(entry => entry.Key.ToString()))
            {
                string name = entry.Key.ToString()!;

                if (Array.Exists(EnvironmentVariablePrefixes, prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                {
                    string sanitizedValue = SanitizeEnvironmentVariable(name, entry.Value);
                    builder.AppendLine($"  {name}={sanitizedValue}");
                }
            }

            builder.AppendLine("Related processes still running:");

            foreach (Process process in Process.GetProcesses().OrderBy(process => process.Id))
            {
                using (process)
                {
                    if (Array.Exists(ProcessNamesToCapture, name => process.ProcessName.Contains(name, StringComparison.OrdinalIgnoreCase)))
                    {
                        builder.AppendLine($"  {process.Id} {process.ProcessName}");
                    }
                }
            }

            // Determines whether the failure reproduces right away. A restore that now succeeds points to a transient (timing/concurrency) cause. A restore
            // that fails again leaves a second binlog and trace, captured against the very same state on disk.
            string[] restoreArguments =
            [
                "restore",
                .. arguments,
                $"-bl:{diagnosticsFilePrefix}.retry-restore.binlog"
            ];

            try
            {
                Dictionary<string, string> retryEnvironmentVariables = new(traceEnvironmentVariables)
                {
                    ["DOTNET_EventPipeOutputPath"] = $"{diagnosticsFilePrefix}.retry-restore.{{pid}}.nettrace"
                };

                string output = await RunDotNetAsync(workingDirectory, 0, retryEnvironmentVariables, restoreArguments);
                builder.AppendLine("Retrying restore succeeded. Output:");
                builder.AppendLine(output);
            }
            catch (Exception exception)
            {
                builder.AppendLine("Retrying restore failed:");
                builder.AppendLine(exception.ToString());
            }
        }
        catch (Exception exception)
        {
            builder.AppendLine($"Failed to capture all context: {exception}");
        }

        await File.WriteAllTextAsync($"{diagnosticsFilePrefix}.context.txt", builder.ToString(), CancellationToken.None);
    }

    private static string SanitizeEnvironmentVariable(string name, object? value)
    {
        return SensitiveVariableRegex().IsMatch(name) ? "******" : value?.ToString() ?? string.Empty;
    }

    private static void DeleteDiagnosticsFiles(string diagnosticsDirectory, string fileNamePrefix)
    {
        foreach (string path in Directory.EnumerateFiles(diagnosticsDirectory, $"{fileNamePrefix}.*"))
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Best-effort cleanup only: a transiently locked file (e.g. an antivirus scan, or a lingering build server) must not fail the test run.
            }
        }
    }

    private static async Task<string> ResolveDiagnosticsDirectoryAsync([CallerFilePath] string sourceFilePath = "")
    {
        string sourceDirectory = Path.GetDirectoryName(sourceFilePath)!;
        string repositoryRoot = await RunGitAsync(sourceDirectory, CancellationToken.None, "rev-parse", "--show-toplevel");
        string diagnosticsDirectory = Path.Combine(repositoryRoot.Replace('/', Path.DirectorySeparatorChar), "TestOutput");
        Directory.CreateDirectory(diagnosticsDirectory);
        return diagnosticsDirectory;
    }

    public static Task<string> RunDotNetAsync(string workingDirectory, int exitCodeExpected, Dictionary<string, string>? environmentVariables,
        params string[] arguments)
    {
        string[] dotNetArguments =
        [
            .. arguments,
            "-p:RunAnalyzers=false",
            "-p:NuGetAudit=false"
        ];

        // Workaround for https://github.com/dotnet/msbuild/issues/6219.
        Dictionary<string, string> dotNetEnvironmentVariables = GetRedirectedTempEnvironmentVariables();

        // Without this, a spawned "dotnet build"/"publish" leaves a persistent MSBuild worker node running in the background for reuse by a later
        // build. That node inherits our redirected stdout/stderr pipe handles and keeps them open after the process we launched exits, so the read end
        // never sees EOF and awaiting exit below would block forever even though the build already completed successfully.
        dotNetEnvironmentVariables["MSBUILDDISABLENODEREUSE"] = "1";

        // Include stack traces when NuGet reports an exception, so that otherwise opaque restore failures (such as MSB4181) become diagnosable.
        dotNetEnvironmentVariables["NUGET_SHOW_STACK"] = "true";

        foreach ((string name, string value) in environmentVariables ?? [])
        {
            dotNetEnvironmentVariables[name] = value;
        }

        return RunAsync("dotnet", workingDirectory, exitCodeExpected, dotNetEnvironmentVariables, TestContext.Current.CancellationToken, dotNetArguments);
    }

    private static Dictionary<string, string> GetRedirectedTempEnvironmentVariables()
    {
        string tempDirectory = PackGitPropertiesSourceOnceFixture.TempDirectory;

        return new Dictionary<string, string>
        {
            ["TMP"] = tempDirectory,
            ["TEMP"] = tempDirectory,
            ["TMPDIR"] = tempDirectory
        };
    }

    public static Task<string> RunPwdAsync(string workingDirectory)
    {
        return RunAsync("pwd", workingDirectory, 0, null, TestContext.Current.CancellationToken, "-P");
    }

    private static async Task<string> RunAsync(string fileName, string workingDirectory, int exitCodeExpected, Dictionary<string, string>? environmentVariables,
        CancellationToken cancellationToken, params string[] arguments)
    {
        var outputBuilder = new StringBuilder();
        Lock outputLock = new();

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        foreach ((string key, string name) in environmentVariables ?? [])
        {
            startInfo.EnvironmentVariables[key] = name;
        }

        using var process = new Process();
        process.StartInfo = startInfo;
        process.OutputDataReceived += (_, eventArgs) => AppendLine(eventArgs.Data);
        process.ErrorDataReceived += (_, eventArgs) => AppendLine(eventArgs.Data);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(ProcessExitTimeout);

        try
        {
            await process.WaitForExitAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException)
        {
            KillEntireProcessTreeInBackground(process.Id);

            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            throw new TimeoutException($"'{fileName} {string.Join(' ', arguments)}' in '{workingDirectory}' did not exit within {ProcessExitTimeout}.");
        }

        string output = outputBuilder.ToString();

        process.ExitCode.Should().Be(exitCodeExpected, "'{0} {1}' in '{2}' was expected to exit with code {3}. Output:\n{4}", fileName,
            string.Join(' ', arguments), workingDirectory, exitCodeExpected, output);

        return output;

        void AppendLine(string? line)
        {
            if (line == null)
            {
                return;
            }

#pragma warning disable S6507 // Blocks should not be synchronized on local variables
            // Justification: Deliberately a call-scoped lock, not a shared static one: a global lock would serialize stdout/stderr callbacks
            // across every concurrently running process, starving the thread pool under high-volume output (e.g. "dotnet build -v:detailed").
            lock (outputLock)
#pragma warning restore S6507 // Blocks should not be synchronized on local variables
            {
                outputBuilder.AppendLine(line);
            }
        }
    }

    private static void KillEntireProcessTreeInBackground(int processId)
    {
        // Fire-and-forget, so that pressing the Stop button in an IDE responds immediately.
        _ = Task.Run(() =>
        {
            try
            {
                using var process = Process.GetProcessById(processId);
                process.Kill(true);
            }
            catch (Exception)
            {
                // Best-effort kill of an already-timed-out process.
            }
        });
    }

    [GeneratedRegex("password|secret|key|token|credential", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveVariableRegex();
}
