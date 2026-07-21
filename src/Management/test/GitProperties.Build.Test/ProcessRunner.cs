// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;

namespace Steeltoe.Management.GitProperties.Build.Test;

internal static class ProcessRunner
{
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

    private static readonly Task<string> RealGitExecutableTask = ResolveGitExecutableAsync();

    private static async Task<string> ResolveGitExecutableAsync()
    {
        string output = await RunAsync(LocatorCommand, Path.GetTempPath(), 0, CancellationToken.None, "git");

        string firstLine = output.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ??
            throw new InvalidOperationException($"Could not resolve the location of git via '{LocatorCommand} git'.");

        return firstLine.Trim();
    }

    public static Task<string> RunGitAsync(string workingDirectory, params string[] arguments)
    {
        return RunGitAsync(workingDirectory, TestContext.Current.CancellationToken, arguments);
    }

    public static async Task<string> RunGitAsync(string workingDirectory, CancellationToken cancellationToken, params string[] arguments)
    {
        string gitExecutable = await RealGitExecutableTask;
        string output = await RunAsync(gitExecutable, workingDirectory, 0, cancellationToken, arguments);
        return output.Trim();
    }

    public static Task<string> RunDotnetAsync(string workingDirectory, params string[] arguments)
    {
        return RunDotnetAsync(workingDirectory, 0, arguments);
    }

    public static Task<string> RunDotnetAsync(string workingDirectory, int exitCodeExpected, params string[] arguments)
    {
        string[] dotnetArguments =
        [
            .. arguments,
            "-p:RunAnalyzers=false",
            "-p:NuGetAudit=false"
        ];

        return RunAsync("dotnet", workingDirectory, exitCodeExpected, TestContext.Current.CancellationToken, dotnetArguments);
    }

    public static Task<string> RunPwdAsync(string workingDirectory)
    {
        return RunAsync("pwd", workingDirectory, 0, TestContext.Current.CancellationToken, "-P");
    }

    private static async Task<string> RunAsync(string fileName, string workingDirectory, int exitCodeExpected, CancellationToken cancellationToken,
        params string[] arguments)
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

        // Without this, a spawned "dotnet build"/"publish" leaves a persistent MSBuild worker node running in the background for reuse by a later
        // build. That node inherits our redirected stdout/stderr pipe handles and keeps them open after the process we launched exits, so the read end
        // never sees EOF and awaiting exit below would block forever even though the build already completed successfully.
        startInfo.EnvironmentVariables["MSBUILDDISABLENODEREUSE"] = "1";

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
}
