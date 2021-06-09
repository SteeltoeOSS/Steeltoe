// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Common.Diagnostics
{
    /// <summary>
    /// A utility to simplify the running of system commands.
    /// </summary>
    public class Command
    {
        /// <summary>
        /// Execute the command and return the result.
        /// </summary>
        /// <param name="command">command to be executed</param>
        /// <param name="workingDirectory">directory in which to execute command</param>
        /// <param name="timeoutMillis">amount of time in milliseconds to wait for command to complete</param>
        /// <returns>command result</returns>
        /// <exception cref="CommandException">if a process can not be started for command</exception>
        public async Task<CommandResult> ExecuteAsync(string command, string workingDirectory = null,
            int timeoutMillis = -1)
        {
            using (var process = new Process())
            {
                var arguments = command.Split(new[] { ' ' }, 2);
                process.StartInfo.FileName = arguments[0];
                if (arguments.Length > 1)
                {
                    process.StartInfo.Arguments = arguments[1];
                }

                if (workingDirectory != null)
                {
                    process.StartInfo.WorkingDirectory = workingDirectory;
                }

                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                var output = new StringBuilder();
                var outputCloseEvent = new TaskCompletionSource<bool>();
                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data is null)
                    {
                        outputCloseEvent.SetResult(true);
                    }
                    else
                    {
                        output.AppendLine(e.Data);
                    }
                };

                var error = new StringBuilder();
                var errorCloseEvent = new TaskCompletionSource<bool>();
                process.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data is null)
                    {
                        errorCloseEvent.SetResult(true);
                    }
                    else
                    {
                        error.AppendLine(e.Data);
                    }
                };

                try
                {
                    if (!process.Start())
                    {
                        throw new Exception($"'{command}' failed to start; no details available");
                    }
                }
                catch (Exception ex)
                {
                    throw new CommandException($"'{command}' failed to start: {ex.Message}", ex);
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                var waitForExit = Task.Run(() => process.WaitForExit(timeoutMillis));
                var processTask = Task.WhenAll(waitForExit, outputCloseEvent.Task, errorCloseEvent.Task);
                if (await Task.WhenAny(Task.Delay(timeoutMillis), processTask) == processTask && waitForExit.Result)
                {
                    return new CommandResult
                    {
                        ExitCode = process.ExitCode, Output = output.ToString(), Error = error.ToString()
                    };
                }

                try
                {
                    process.Kill();
                }
                catch
                {
                    // ignore
                }

                throw new Exception($"'{process.StartInfo.FileName} {process.StartInfo.Arguments}' timed out");
            }
        }
    }

    public struct CommandResult
    {
        public int ExitCode { get; internal set; }
        public string Output { get; internal set; }
        public string Error { get; internal set; }
    }

    public class CommandException : Exception
    {
        public CommandException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}