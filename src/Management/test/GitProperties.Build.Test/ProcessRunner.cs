// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;

namespace Steeltoe.Management.GitProperties.Build.Test;

/// <summary>
/// Runs external processes (git, dotnet) and captures stdout/stderr merged into a single string - tests assert on substrings (diagnostic codes,
/// "Skipping target", etc.) that can land on either stream.
/// </summary>
internal static class ProcessRunner
{
    private static readonly string LocatorCommand = OperatingSystem.IsWindows() ? "where" : "which";

    private static readonly char[] LineSeparators =
    [
        '\r',
        '\n'
    ];

    /// <summary>
    /// Generous enough to comfortably cover the slowest single command this suite ever runs (a Release build plus NuGet pack, or a "dotnet build" against a
    /// cold/isolated restore) even under heavy system load, while still turning a genuine hang - e.g. a lingering process holding the redirected output pipe
    /// open, the exact failure mode <see cref="RunAsync" />'s own MSBUILDDISABLENODEREUSE setting guards against - into a fast, informative test failure
    /// instead of blocking the whole suite indefinitely.
    /// </summary>
    private static readonly TimeSpan ProcessExitTimeout = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Started once, eagerly, the moment this class is first touched - every caller (RunGitAsync and, transitively, everything else here) awaits this same
    /// Task instead of re-resolving "where git" on every single call, or blocking a thread synchronously on it.
    /// </summary>
    private static readonly Task<string> RealGitExecutableTask = ResolveGitExecutableAsync();

    /// <summary>
    /// The single, shared place every process this suite ever spawns gets its exit code checked - required, not defaulted, specifically so every one of
    /// RunGitAsync, RunDotnetAsync, and RunPwdAsync has to make its own success expectation explicit, rather than silently inheriting whatever the last
    /// caller happened to pass. Private: every real caller in this project goes through one of those three named, purpose-specific wrappers instead of this
    /// directly. A silently-ignored failure here is exactly what let a broken "git remote set-url" call (rejected by git itself, exit code 128, because the
    /// remote already had multiple values) pass unnoticed until a much later, more confusing assertion failed on a stale property value instead.
    /// </summary>
    private static async Task<string> RunAsync(string fileName, string workingDirectory, int exitCodeExpected, CancellationToken cancellationToken,
        params string[] arguments)
    {
        var outputBuilder = new StringBuilder();
        object outputLock = new();

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

        // Without this, a spawned "dotnet build"/"publish" leaves a persistent MSBuild worker node
        // running in the background for reuse by a later build (the SDK's default, off a dev
        // machine with no CI environment variable set). That node inherits our redirected
        // stdout/stderr pipe handles and keeps them open even after the process we launched here
        // exits - so the read end never sees EOF, and awaiting exit below would otherwise block
        // forever waiting for a pipe close that will never happen, even though the build already
        // completed successfully.
        startInfo.EnvironmentVariables["MSBUILDDISABLENODEREUSE"] = "1";

        using var process = new Process();
        process.StartInfo = startInfo;
        process.OutputDataReceived += (_, eventArgs) => AppendLine(eventArgs.Data);
        process.ErrorDataReceived += (_, eventArgs) => AppendLine(eventArgs.Data);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Linked, not just our own timeout: composes the caller's own cancellation (e.g. xUnit
        // cancelling the test run via TestContext.Current.CancellationToken) with our internal
        // "this must be a hung pipe" timeout, without one silently overriding the other.
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(ProcessExitTimeout);

        try
        {
            // WaitForExitAsync, unlike the synchronous WaitForExit, never ties up a thread pool
            // thread for the (up to) two minutes this can wait - it composes with the timeout purely
            // through cancellation instead.
            await process.WaitForExitAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException)
        {
            KillEntireProcessTreeInBackground(process.Id);

            if (cancellationToken.IsCancellationRequested)
            {
                // The caller's own token fired, not our internal timeout - propagate that as-is
                // rather than obscuring a genuine cancellation behind a misleading TimeoutException.
                throw;
            }

            throw new TimeoutException(
                $"'{fileName} {string.Join(' ', arguments)}' in '{workingDirectory}' did not exit within {ProcessExitTimeout} - probably a hung " +
                "process holding the redirected output pipe open rather than the command itself still genuinely running.");
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

            // Deliberately a call-scoped lock, not a shared static one: many tests run processes
            // concurrently (parallel test execution, each spawning further child processes such as
            // MSBuild worker nodes or the git shim), and every process's stdout/stderr callback
            // would otherwise contend for one single global lock. Under enough concurrent,
            // high-volume output (e.g. "dotnet build -v:detailed"), that starves the thread pool
            // and stalls the whole suite indefinitely - this lock only ever guards this one call's
            // own StringBuilder.
#pragma warning disable S6507
            lock (outputLock)
#pragma warning restore S6507
            {
                outputBuilder.AppendLine(line);
            }
        }
    }

    /// <summary>
    /// Fire-and-forget, not awaited: <see cref="Process.Kill(bool)" /> with
    /// <c>
    /// entireProcessTree: true
    /// </c>
    /// walks every process on the machine to find descendants by parent-PID/start-time matching, which measurably takes several seconds on a machine with a
    /// typical number of processes running - blocking the cancellation/timeout path on that would make a cancelled test appear to hang for that same several
    /// seconds even though the test itself already stopped waiting. Re-resolves the process by id, rather than closing over the original (disposed by
    /// <see cref="RunAsync" />'s own "using") <see cref="Process" /> instance, since calling a method on an already-disposed instance from this background
    /// task would throw. Best-effort only: a failure here (the process already exited, or its id got reused by an unrelated process) must never surface
    /// anywhere, since nothing awaits this.
    /// </summary>
    private static void KillEntireProcessTreeInBackground(int processId)
    {
        _ = Task.Run(() =>
        {
            try
            {
                using var process = Process.GetProcessById(processId);
                process.Kill(true);
            }
            catch (Exception)
            {
                // Intentionally left empty.
            }
        });
    }

    /// <summary>
    /// Uses the currently-running test's own TestContext.Current.CancellationToken - the right default for every call site in this suite except one: see the
    /// explicit-CancellationToken overload below for why TestPaths.ResolveRepositoryRootAsync can't use this one.
    /// </summary>
    public static Task<string> RunGitAsync(string workingDirectory, params string[] arguments)
    {
        return RunGitAsync(workingDirectory, TestContext.Current.CancellationToken, arguments);
    }

    /// <summary>
    /// Explicit-CancellationToken overload, used only by TestPaths.ResolveRepositoryRootAsync's shared, fire-once resolution with CancellationToken.None -
    /// see that method's own remarks (and <see cref="ResolveGitExecutableAsync" />'s, for the identical reasoning) for why a specific test's
    /// TestContext.Current.CancellationToken would be wrong there.
    /// </summary>
    public static async Task<string> RunGitAsync(string workingDirectory, CancellationToken cancellationToken, params string[] arguments)
    {
        string gitExecutable = await RealGitExecutableTask;
        return await RunAsync(gitExecutable, workingDirectory, 0, cancellationToken, arguments);
    }

    /// <summary>
    /// Defaults to expecting success (exit code 0), symmetric with <see cref="RunGitAsync(string,string[])" /> - see the explicit-exit-code overload below
    /// for the handful of tests that deliberately provoke a build/publish failure as the scenario under test.
    /// </summary>
    public static Task<string> RunDotnetAsync(string workingDirectory, params string[] arguments)
    {
        return RunDotnetAsync(workingDirectory, 0, arguments);
    }

    /// <summary>
    /// For the rare test that provokes a specific dotnet build/publish failure as the very scenario under test (e.g.
    /// WriteGitPropertiesFallbackFileThenPublishNoBuildFailsTest).
    /// </summary>
    public static Task<string> RunDotnetAsync(string workingDirectory, int exitCodeExpected, params string[] arguments)
    {
        return RunAsync("dotnet", workingDirectory, exitCodeExpected, TestContext.Current.CancellationToken, BuildDotnetArguments(arguments));
    }

    /// <summary>
    /// Always appends "-p:RunAnalyzers=false" and "-p:NuGetAudit=false" - measured to save only ~0.4-0.5s on a standalone build of the (tiny) task assembly,
    /// and within run-to-run noise once folded into a real end-to-end TestApp build, but harmless either way here: no test in this suite asserts on analyzer
    /// diagnostics or NuGet audit warnings, only on this project's own GITPROPS0xx codes and plain build success/failure.
    /// </summary>
    private static string[] BuildDotnetArguments(string[] arguments)
    {
        return
        [
            .. arguments,
            "-p:RunAnalyzers=false",
            "-p:NuGetAudit=false"
        ];
    }

    public static async Task<string> GetGitOutputAsync(string workingDirectory, params string[] arguments)
    {
        string output = await RunGitAsync(workingDirectory, arguments);
        return output.Trim();
    }

    /// <summary>
    /// Used only by GitPropertiesTestWorkspace's macOS-only $TMPDIR symlink resolution (see its own remarks) - "pwd -P" is unrelated to git/dotnet, so this
    /// runs it directly rather than forcing that through RunGitAsync/RunDotnetAsync.
    /// </summary>
    public static Task<string> RunPwdAsync(string workingDirectory)
    {
        return RunAsync("pwd", workingDirectory, 0, TestContext.Current.CancellationToken, "-P");
    }

    /// <summary>
    /// Deliberately CancellationToken.None, not a specific test's TestContext.Current.CancellationToken: <see cref="RealGitExecutableTask" /> is a single,
    /// process-wide resource shared by every test class running concurrently, resolved once by whichever test happens to touch this class first - tying that
    /// one-time resolution to that particular (arbitrary, unrelated) test's cancellation would be wrong, since cancelling THAT test must not cancel
    /// resolution for every OTHER test still waiting on the same shared Task.
    /// </summary>
    private static async Task<string> ResolveGitExecutableAsync()
    {
        string output = await RunAsync(LocatorCommand, Path.GetTempPath(), 0, CancellationToken.None, "git");

        string firstLine = output.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ??
            throw new InvalidOperationException($"Could not resolve the location of git via '{LocatorCommand} git'.");

        return firstLine.Trim();
    }
}
