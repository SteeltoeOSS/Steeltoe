// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Steeltoe.Management.GitProperties.Build;

// ReSharper disable once UnusedType.Global
public sealed class ReportGitDiagnosticTask : Task
{
    /// <summary>
    /// Gets or sets the numeric part of the diagnostic code to report.
    /// </summary>
    [Required]
    public int DiagnosticId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to report at warning level, rather than as a code-carrying informational message.
    /// </summary>
    public bool EnableWarnings { get; set; }

    /// <summary>
    /// Gets or sets the diagnostic's body text.
    /// </summary>
    [Required]
    public string Message { get; set; } = string.Empty;

    /// <inheritdoc />
    public override bool Execute()
    {
        // Using the <Message /> built-in MSBuild task provides no way to set the Code property.
        GitDiagnosticReporter.Report(Log, DiagnosticId, EnableWarnings, Message);
        return true;
    }
}
