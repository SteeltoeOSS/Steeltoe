// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Steeltoe.Management.GitProperties.Build;

/// <summary>
/// Merges the shared git.properties cache with the fields that can never be cached: the live repository dirty state, the per-project $(Version), and the
/// build's own timestamp. Runs every build with no Inputs/Outputs skip: editing a tracked file doesn't touch any file timestamp this task could key
/// incrementality off, and a cached build time would go stale the moment it's reused.
/// </summary>
// ReSharper disable once UnusedType.Global
public sealed class ComposeGitPropertiesTask : Task
{
    /// <summary>
    /// Gets or sets the resolved git repository root directory.
    /// </summary>
    [Required]
    public string RepositoryRoot { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the git executable to invoke.
    /// </summary>
    [Required]
    public string GitExecutable { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the shared cache file to read from.
    /// </summary>
    [Required]
    public string CacheFile { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the per-project output file to write.
    /// </summary>
    [Required]
    public string OutputFile { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the consuming project's $(Version), written as git.build.version.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets an optional additional path to copy the composed git.properties to. This is the durable fallback file used when a build has no usable
    /// git repository at all (e.g. a source-based `cf push`, where .git is excluded from the pushed tree).
    /// </summary>
    public string? FallbackFile { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a git executable failure is reported as a warning (true) or an informational message (false).
    /// </summary>
    public bool EnableWarnings { get; set; }

    /// <inheritdoc />
    public override bool Execute()
    {
        bool? isDirty = DetermineDirtyState();
        List<string> lines = [];

        if (this.LogOnFailure($"failed to read {CacheFile}", () => lines = AtomicFile.Read(CacheFile).ToList()))
        {
            if (isDirty == true)
            {
                for (int index = 0; index < lines.Count; index++)
                {
                    if (lines[index].StartsWith($"{GitPropertiesFormat.CommitIdDescribeKey}=", StringComparison.Ordinal))
                    {
                        lines[index] += "-dirty";
                    }
                }
            }

            if (isDirty != null)
            {
                lines.Add($"git.dirty={(isDirty.Value ? "true" : "false")}");
            }

            lines.Add($"git.build.version={GitPropertiesFormat.EscapeLineBreaks(Version)}");

            // Local time, not UTC, to match the ISO-8601-with-offset style git itself uses for git.commit.time. This is "when this build ran, in the
            // machine's own local time", not a value that needs to compare against the commit's own timestamp.
#pragma warning disable S6354
            string buildTime = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
#pragma warning restore S6354
            lines.Add($"git.build.time={buildTime}");

            if (this.LogOnFailure($"failed to write {OutputFile}", () => AtomicFile.Write(OutputFile, lines)))
            {
                if (FallbackFile is null or "")
                {
                    return true;
                }

                return this.LogOnFailure($"failed to write fallback file {FallbackFile}", () => AtomicFile.Write(FallbackFile, lines));
            }
        }

        return false;
    }

    private bool? DetermineDirtyState()
    {
        const string dirtyCheckArguments = "status --porcelain";

        int exitCode;
        string stdout;

        try
        {
            exitCode = GitProcessRunner.Run(GitExecutable, RepositoryRoot, dirtyCheckArguments, out stdout, out _);
        }
        catch (Exception exception)
        {
            GitDiagnosticReporter.Report(Log, GitDiagnosticId.GitDirtyStateUnknown, EnableWarnings,
                $"git.properties: unable to determine dirty state because '{GitExecutable}' failed ({exception.Message}).");

            return null;
        }

        if (exitCode != 0)
        {
            GitDiagnosticReporter.Report(Log, GitDiagnosticId.GitDirtyStateUnknown, EnableWarnings,
                $"git.properties: unable to determine dirty state because '{GitExecutable} {dirtyCheckArguments}' exited with code {exitCode}.");

            return null;
        }

        return stdout.Length > 0;
    }
}
