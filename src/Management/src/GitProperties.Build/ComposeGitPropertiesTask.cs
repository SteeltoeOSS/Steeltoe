// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Steeltoe.Management.GitProperties.Build;

/// <summary>
/// Merges the shared cache (see <see cref="GenerateGitPropertiesCacheTask" />) with the fields that can never be cached across projects: the live
/// working-tree dirty state, the per-project $(Version), and the current build's own timestamp. Runs once per project, every build - unlike the cache
/// generation, this is deliberately not skippable via Inputs/Outputs, since editing a tracked file doesn't touch any file timestamp this task could key
/// incrementality off, and a cached build time would go stale the moment it's reused by a second build.
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
    /// Gets or sets an optional additional path to copy the same composed content to - the durable fallback file that
    /// Steeltoe.Management.GitProperties.Build.targets' IncludeGitPropertiesInOutput target later falls back to when a build has no usable git repository at
    /// all (e.g. a source-based `cf push`, where .git is excluded from the pushed tree by default). Empty (the default) is a no-op; only non-empty when the
    /// consumer opted into $(GitPropertiesWriteToProjectDirectory).
    /// </summary>
    public string? FallbackFile { get; set; }

    /// <inheritdoc />
    public override bool Execute()
    {
        int exitCode = GitProcessRunner.Run(GitExecutable, RepositoryRoot, "status --porcelain", out string stdout, out string stderr);

        if (exitCode != 0)
        {
            Log.LogError("git.properties: failed to determine working tree status: {0}", stderr);
            return false;
        }

        bool isDirty = stdout.Length > 0;
        List<string> lines;

        try
        {
            lines = AtomicFile.ReadAllLinesWithRetry(CacheFile).ToList();
        }
        catch (IOException exception)
        {
            Log.LogError($"git.properties: failed to read {CacheFile}: {exception.Message}");
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

        // S6354 (use an injectable time provider): not practical here, for the same reason as
        // AtomicFile.TryAcquireExclusiveLock - see that method's remarks. Matches the
        // ISO-8601-with-offset style git itself uses for git.commit.time (%cI), rather than
        // normalizing to UTC - this is "when this build ran, in the build machine's own local time",
        // not a value that needs to compare directly against the commit's own timestamp.
#pragma warning disable S6354
        string buildTime = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
#pragma warning restore S6354
        lines.Add($"git.build.time={buildTime}");

        try
        {
            AtomicFile.WriteAtomic(OutputFile, lines);
        }
        catch (IOException exception)
        {
            Log.LogError($"git.properties: failed to write {OutputFile}: {exception.Message}");
            return false;
        }

        if (FallbackFile is null or "")
        {
            return true;
        }

        try
        {
            AtomicFile.WriteAtomic(FallbackFile, lines);
        }
        catch (IOException exception)
        {
            Log.LogError($"git.properties: failed to write fallback file {FallbackFile}: {exception.Message}");
            return false;
        }

        return true;
    }
}
