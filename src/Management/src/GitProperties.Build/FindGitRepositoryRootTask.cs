// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Steeltoe.Management.GitProperties.Build;

/// <summary>
/// Walks up from <see cref="StartDirectory" /> looking for a ".git" directory (a regular repository) or file (used by worktrees and submodules).
/// </summary>
// ReSharper disable once UnusedType.Global
public sealed class FindGitRepositoryRootTask : Task
{
    /// <summary>
    /// Gets or sets the directory to start walking up from.
    /// </summary>
    [Required]
    public string StartDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether a forgivable anomaly is reported as a warning (true) or an informational message (false).
    /// </summary>
    [Required]
    public bool EnableWarnings { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to suppress the diagnostic that no usable .git directory was found.
    /// </summary>
    [Required]
    public bool SuppressGitRepositoryNotFound { get; set; }

    /// <summary>
    /// Gets or sets the resolved repository root, or empty when none was found.
    /// </summary>
    [Output]
    public string RepositoryRoot { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the directory that holds "HEAD".
    /// </summary>
    [Output]
    public string HeadGitDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the directory that holds "config", "packed-refs", and "refs".
    /// </summary>
    [Output]
    public string CommonGitDirectory { get; set; } = string.Empty;

    /// <inheritdoc />
    public override bool Execute()
    {
        bool succeeded = this.LogOnFailure($"failed to walk up from '{StartDirectory}' looking for a git repository", () =>
        {
            GitDirectories? directories = null;
            var current = new DirectoryInfo(StartDirectory);

            while (current != null && directories == null)
            {
                string gitPath = Path.Combine(current.FullName, ".git");

                if (Directory.Exists(gitPath))
                {
                    directories = GitDirectories.Create(current.FullName, gitPath, gitPath);
                }
                else if (File.Exists(gitPath))
                {
                    directories = ResolveGitFile(gitPath, current.FullName);
                }
                else
                {
                    current = current.Parent;
                }
            }

            if (directories == null && !SuppressGitRepositoryNotFound)
            {
                GitDiagnosticReporter.Report(Log, GitDiagnosticId.GitRepositoryNotFound, EnableWarnings,
                    $"git.properties generation skipped: no usable .git directory found in or above '{StartDirectory}'.");
            }

            RepositoryRoot = directories?.RootGitDirectory ?? string.Empty;
            HeadGitDirectory = directories?.HeadGitDirectory ?? string.Empty;
            CommonGitDirectory = directories?.CommonGitDirectory ?? string.Empty;
        });

        return succeeded;
    }

    private GitDirectories ResolveGitFile(string gitFilePath, string baseDirectory)
    {
        // A .git file (as used by worktrees and submodules) is resolved via its "gitdir:" pointer.
        // For a worktree, that leads to a private directory holding "HEAD", which points at the directory shared by every worktree via a "commondir" file.
        // For a submodule, or anything else, the two are the same directory.

        string? headGitDirectory = ReadGitDirPointer(gitFilePath, baseDirectory);
        string? commonGitDirectory = headGitDirectory == null ? null : ResolveCommonGitDirectory(headGitDirectory);

        if (headGitDirectory != null && commonGitDirectory != null)
        {
            return GitDirectories.Create(baseDirectory, headGitDirectory, commonGitDirectory);
        }

        GitDiagnosticReporter.Report(Log, GitDiagnosticId.GitRepositoryInvalid, EnableWarnings,
            $"git.properties generation skipped: failed to resolve the worktree or submodule reference at '{gitFilePath}'. It may be corrupted, moved, or deleted.");

        return GitDirectories.Invalid;
    }

    private static string? ReadGitDirPointer(string gitFilePath, string baseDirectory)
    {
        const string gitDirPrefix = "gitdir:";
        string content = File.ReadAllText(gitFilePath).Trim();

        if (content.StartsWith(gitDirPrefix, StringComparison.Ordinal))
        {
            string path = content.Substring(gitDirPrefix.Length).Trim();

            if (path.Length != 0)
            {
                return ResolveExistingDirectory(baseDirectory, path);
            }
        }

        return null;
    }

    private static string? ResolveCommonGitDirectory(string headGitDirectory)
    {
        string commonDirFile = Path.Combine(headGitDirectory, "commondir");

        if (!File.Exists(commonDirFile))
        {
            return headGitDirectory;
        }

        string path = File.ReadAllText(commonDirFile).Trim();
        return path.Length == 0 ? null : ResolveExistingDirectory(headGitDirectory, path);
    }

    private static string? ResolveExistingDirectory(string baseDirectory, string path)
    {
        string directory = Path.GetFullPath(Path.Combine(baseDirectory, path));
        return Directory.Exists(directory) ? directory : null;
    }

    private sealed class GitDirectories
    {
        public static GitDirectories Invalid { get; } = new(string.Empty, string.Empty, string.Empty);

        public string RootGitDirectory { get; }
        public string HeadGitDirectory { get; }
        public string CommonGitDirectory { get; }

        private GitDirectories(string rootGitDirectory, string headGitDirectory, string commonGitDirectory)
        {
            RootGitDirectory = EnsureTrailingPathSeparator(rootGitDirectory);
            HeadGitDirectory = EnsureTrailingPathSeparator(headGitDirectory);
            CommonGitDirectory = EnsureTrailingPathSeparator(commonGitDirectory);
        }

        private static string EnsureTrailingPathSeparator(string path)
        {
            return path.Length > 0 && path[path.Length - 1] != Path.DirectorySeparatorChar ? $"{path}{Path.DirectorySeparatorChar}" : path;
        }

        public static GitDirectories Create(string rootGitDirectory, string headGitDirectory, string commonGitDirectory)
        {
            return new GitDirectories(rootGitDirectory, headGitDirectory, commonGitDirectory);
        }
    }
}
