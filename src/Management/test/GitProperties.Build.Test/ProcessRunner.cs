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

    private static readonly char[]? LineSeparators =
    [
        '\r',
        '\n'
    ];

    public static readonly string RealGitExecutable = ResolveGitExecutable();

    public static ProcessResult Run(string fileName, string workingDirectory, params string[] arguments)
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

        using var process = new Process();
        process.StartInfo = startInfo;
        process.OutputDataReceived += (_, eventArgs) => AppendLine(eventArgs.Data);
        process.ErrorDataReceived += (_, eventArgs) => AppendLine(eventArgs.Data);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        string output = outputBuilder.ToString();
        return new ProcessResult(process.ExitCode, output);

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

    public static ProcessResult RunGit(string workingDirectory, params string[] arguments)
    {
        return Run(RealGitExecutable, workingDirectory, arguments);
    }

    public static ProcessResult RunDotnet(string workingDirectory, params string[] arguments)
    {
        return Run("dotnet", workingDirectory, arguments);
    }

    public static string GetGitOutput(string workingDirectory, params string[] arguments)
    {
        ProcessResult result = RunGit(workingDirectory, arguments);
        return result.Output.Trim();
    }

    private static string ResolveGitExecutable()
    {
        ProcessResult result = Run(LocatorCommand, Path.GetTempPath(), "git");

        string firstLine = result.Output.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ??
            throw new InvalidOperationException($"Could not resolve the location of git via '{LocatorCommand} git'.");

        return firstLine.Trim();
    }
}
