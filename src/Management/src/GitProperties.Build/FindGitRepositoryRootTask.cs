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
/// Walks up from <see cref="StartDirectory" /> looking for a ".git" directory. A ".git" file (worktree or submodule pointer) is deliberately reported
/// back via <see cref="IsUnsupportedGitFile" /> rather than treated as a match.
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
    /// Gets or sets the resolved repository root directory, or empty when none was found.
    /// </summary>
    [Output]
    public string RepositoryRoot { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether a ".git" file (rather than a directory) was found.
    /// </summary>
    [Output]
    public bool IsUnsupportedGitFile { get; set; }

    /// <inheritdoc />
    public override bool Execute()
    {
        string repositoryRoot = string.Empty;
        bool isUnsupportedGitFile = false;

        return this.LogOnFailure($"failed to walk up from '{StartDirectory}' looking for a git repository root", () =>
        {
            var current = new DirectoryInfo(StartDirectory);

            while (current != null)
            {
                string gitPath = Path.Combine(current.FullName, ".git");

                if (Directory.Exists(gitPath))
                {
                    repositoryRoot = current.FullName;

                    if (repositoryRoot.Length > 0 && repositoryRoot[repositoryRoot.Length - 1] != Path.DirectorySeparatorChar)
                    {
                        repositoryRoot = string.Concat(repositoryRoot, Path.DirectorySeparatorChar);
                    }

                    break;
                }

                if (File.Exists(gitPath))
                {
                    isUnsupportedGitFile = true;
                    break;
                }

                current = current.Parent;
            }

            RepositoryRoot = repositoryRoot;
            IsUnsupportedGitFile = isUnsupportedGitFile;
        });
    }
}
