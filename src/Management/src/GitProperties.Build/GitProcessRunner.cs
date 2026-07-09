// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;

namespace Steeltoe.Management.GitProperties.Build;

internal static class GitProcessRunner
{
    public static int Run(string gitExecutable, string repositoryRoot, string arguments, out string stdout, out string stderr)
    {
        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();

        var startInfo = new ProcessStartInfo
        {
            FileName = gitExecutable,
            Arguments = arguments,
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process();
        process.StartInfo = startInfo;
        process.OutputDataReceived += (_, eventArgs) => AppendLine(stdoutBuilder, eventArgs.Data);
        process.ErrorDataReceived += (_, eventArgs) => AppendLine(stderrBuilder, eventArgs.Data);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        stdout = stdoutBuilder.ToString().Trim();
        stderr = stderrBuilder.ToString().Trim();
        return process.ExitCode;
    }

    private static void AppendLine(StringBuilder builder, string? line)
    {
        if (line == null)
        {
            return;
        }

        // git itself always writes \n line endings on its own output (even on Windows).
        builder.Append(line).Append('\n');
    }
}
