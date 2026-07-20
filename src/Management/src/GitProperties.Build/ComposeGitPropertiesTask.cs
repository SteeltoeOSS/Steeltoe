// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Steeltoe.Management.GitProperties.Build;

/// <summary>
/// Merges the shared git.properties cache with the fields that can never be cached: the live working-tree dirty state, the per-project $(Version), and
/// the build's own timestamp. Runs every build with no Inputs/Outputs skip: editing a tracked file doesn't touch any file timestamp this task could key
/// incrementality off, and a cached build time would go stale the moment it's reused. Every failure returns <c>false</c>, so a partial run can't leave
/// output mismatched with what's on disk for a later step to pick up.
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

    /// <inheritdoc />
    public override bool Execute()
    {
        int exitCode;
        string stdout;
        string stderr;

        try
        {
            exitCode = GitProcessRunner.Run(GitExecutable, RepositoryRoot, "status --porcelain", out stdout, out stderr);
        }
        catch (Exception exception)
        {
            Log.LogError($"git.properties: an unexpected error occurred while determining working tree status:{Environment.NewLine}{exception}");
            return false;
        }

        if (exitCode != 0)
        {
            Log.LogError("git.properties: failed to determine working tree status: {0}", stderr);
            return false;
        }

        bool isDirty = stdout.Length > 0;
        List<string> lines = [];

        if (!TryRunFileOperation($"read {CacheFile}", () => lines = AtomicFile.Read(CacheFile).ToList()))
        {
            return false;
        }

        for (int index = 0; index < lines.Count; index++)
        {
            if (isDirty && lines[index].StartsWith($"{GitPropertiesFormat.CommitIdDescribeKey}=", StringComparison.Ordinal))
            {
                lines[index] += "-dirty";
            }
        }

        lines.Add($"git.dirty={(isDirty ? "true" : "false")}");
        lines.Add($"git.build.version={GitPropertiesFormat.EscapeLineBreaks(Version)}");

        // Local time, not UTC, to match the ISO-8601-with-offset style git itself uses for git.commit.time. This is "when this build ran, in the
        // machine's own local time", not a value that needs to compare against the commit's own timestamp.
#pragma warning disable S6354
        string buildTime = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
#pragma warning restore S6354
        lines.Add($"git.build.time={buildTime}");

        if (!TryRunFileOperation($"write {OutputFile}", () => AtomicFile.Write(OutputFile, lines)))
        {
            return false;
        }

        if (FallbackFile is null or "")
        {
            return true;
        }

        return TryRunFileOperation($"write fallback file {FallbackFile}", () => AtomicFile.Write(FallbackFile, lines));
    }

    private bool TryRunFileOperation(string description, Action action)
    {
        try
        {
            action();
            return true;
        }
        catch (Exception exception)
        {
            Log.LogError($"git.properties: failed to {description}:{Environment.NewLine}{exception}");
            return false;
        }
    }
}
