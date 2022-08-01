// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Utils.Diagnostics;

/// <summary>
/// A utility abstraction to simplify the running of commands.
/// </summary>
public interface ICommandExecutor
{
    /// <summary>
    /// Execute the command and return the result.
    /// </summary>
    /// <param name="command">Command to be executed.</param>
    /// <param name="workingDirectory">The directory that contains the command process.</param>
    /// <param name="timeout">The amount of time in milliseconds to wait for command to complete.</param>
    /// <returns>Command result.</returns>
    /// <exception cref="CommandException">If a process can not be started for command.</exception>
    Task<CommandResult> ExecuteAsync(string command, string workingDirectory = null, int timeout = -1);
}
