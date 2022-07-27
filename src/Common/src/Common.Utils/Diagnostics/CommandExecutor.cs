// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Common.Utils.Diagnostics;

/// <inheritdoc/>
public class CommandExecutor : ICommandExecutor
{
    private static int _commandCounter;

    private readonly ILogger<CommandExecutor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandExecutor"/> class.
    /// </summary>
    /// <param name="logger">Injected logger.</param>
    public CommandExecutor(ILogger<CommandExecutor> logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<CommandResult> ExecuteAsync(string command, string workingDirectory = null, int timeout = -1)
    {
        var commandId = NextCommandId();
        using var process = new Process();
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

        _logger?.LogDebug("[{CommandId}] command: {Command}", commandId, command);
        try
        {
            if (!process.Start())
            {
                _logger?.LogDebug("[{CommandId}] failed to start: {Error}", commandId, "no details available");
                throw new CommandException($"'{command}' failed to start; no details available");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogDebug("[{CommandId}] failed to start: {Error}", commandId, ex.Message);
            throw new CommandException($"'{command}' failed to start: {ex.Message}", ex);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // ReSharper disable once AccessToDisposedClosure
        var waitForExit = Task.Run(() => process.WaitForExit(timeout));
        var processTask = Task.WhenAll(waitForExit, outputCloseEvent.Task, errorCloseEvent.Task);
        if (await Task.WhenAny(Task.Delay(timeout), processTask) == processTask && waitForExit.Result)
        {
            var result = new CommandResult
            {
                ExitCode = process.ExitCode, Output = output.ToString(), Error = error.ToString(),
            };
            _logger?.LogDebug("[{CommandId}] exit code: {ExitCode}", commandId, result.ExitCode);
            if (result.Output.Length > 0)
            {
                _logger?.LogDebug("[{CommandId}] stdout:\n{Output}", commandId, result.Output);
            }

            if (result.Error.Length > 0)
            {
                _logger?.LogDebug("[{CommandId}] stderr:\n{Error}", commandId, result.Error);
            }

            return result;
        }

        try
        {
            process.Kill();
        }
        catch
        {
            // ignore
        }

        _logger?.LogDebug("[{CommandId}] timed out: {TimeOut}ms", commandId, timeout);
        throw new CommandException($"'{process.StartInfo.FileName} {process.StartInfo.Arguments}' timed out");
    }

    private static int NextCommandId()
    {
        return ++_commandCounter;
    }
}