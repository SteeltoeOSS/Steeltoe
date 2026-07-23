// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

internal sealed class GitDiagnosticId
{
    public static GitDiagnosticId GitRepositoryNotFound { get; } = new(1);
    public static GitDiagnosticId GitWorktreeFound { get; } = new(2);
    public static GitDiagnosticId GitExecutableNotFound { get; } = new(3);
    public static GitDiagnosticId IncompatibleGitVersion { get; } = new(4);
    public static GitDiagnosticId GitRepositoryHasNoCommits { get; } = new(5);
    public static GitDiagnosticId GitRepositoryIsShallowClone { get; } = new(6);
    public static GitDiagnosticId GitDirtyStateUnknown { get; } = new(7);

    public int Value { get; }

    private GitDiagnosticId(int value)
    {
        Value = value;
    }
}
