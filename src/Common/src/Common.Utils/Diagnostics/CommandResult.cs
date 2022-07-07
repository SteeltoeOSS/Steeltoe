// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Utils.Diagnostics;

/// <summary>
/// An simple representation of a command result.
/// </summary>
public struct CommandResult
{
    /// <summary>
    /// Gets or sets the command exit code.
    /// </summary>
    public int ExitCode { get; set; }

    /// <summary>
    /// Gets or sets the command exit STDOUT.
    /// </summary>
    public string Output { get; set; }

    /// <summary>
    /// Gets or sets the command exit STDERR.
    /// </summary>
    public string Error { get; set; }
}
