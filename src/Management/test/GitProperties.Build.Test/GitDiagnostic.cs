// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

internal sealed class GitDiagnostic
{
    public static GitDiagnostic GitRepositoryNotFound { get; } = new(1, "no usable .git directory found");
    public static GitDiagnostic UnresolvableGitFile { get; } = new(2, "failed to resolve the worktree or submodule reference");
    public static GitDiagnostic NotInsideUsableGitRepository { get; } = new(2, "not inside a usable git repository");
    public static GitDiagnostic GitExecutableNotFound { get; } = new(3, "could not run");
    public static GitDiagnostic GitVersionCheckExitsNonZero { get; } = new(3, "exited with code");
    public static GitDiagnostic IncompatibleGitVersion { get; } = new(4);
    public static GitDiagnostic GitRepositoryHasNoCommits { get; } = new(5);
    public static GitDiagnostic GitRepositoryIsShallowClone { get; } = new(6);
    public static GitDiagnostic GitDirtyStateUnknown { get; } = new(7, "failed (");
    public static GitDiagnostic GitDirtyCheckExitsNonZero { get; } = new(7, "exited with code");

    public int Code { get; }
    public string? MessageSnippet { get; }

    private GitDiagnostic(int code, string? messageSnippet = null)
    {
        Code = code;
        MessageSnippet = messageSnippet;
    }
}
