// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

/// <summary>
/// The result of <see cref="GitRepository.SimulatePush" /> - a directory tree with no ".git" anywhere in its ancestry, the way a source-based `cf push`
/// actually delivers a project. Deliberately not a <see cref="GitRepository" /> itself: nothing here is a git repository, so none of that type's
/// git-invoking members would make sense on it.
/// </summary>
internal sealed class RemotePushProjectTree(string rootDirectory, TestProject testApp)
{
    public string RootDirectory { get; } = rootDirectory;
    public TestProject TestApp { get; } = testApp;

    public bool HasGitDirectory => Directory.Exists(Path.Combine(RootDirectory, ".git"));
}
