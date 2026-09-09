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
}
