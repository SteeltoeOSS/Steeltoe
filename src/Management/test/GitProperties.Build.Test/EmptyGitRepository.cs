// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

/// <summary>
/// A git repository directory with no TestApp (or any other project) written into it yet - just "git init", so a test can run fully custom git commands
/// (config, commits, tags...) before any project files exist, without those files ending up swept into whatever it commits along the way. Created by
/// <see cref="GitPropertiesTestWorkspace.CreateEmptyRepositoryAsync" /> - never directly. Call <see cref="AddTestAppAsync" /> once ready to build
/// against it, which upgrades this into a full <see cref="GitRepository" />.
/// </summary>
internal sealed class EmptyGitRepository(GitPropertiesTestWorkspace workspace, string rootDirectory)
{
    public string RootDirectory { get; } = rootDirectory;

    public Task<string> RunGitAsync(params string[] arguments)
    {
        return ProcessRunner.RunGitAsync(RootDirectory, arguments);
    }

    /// <summary>
    /// Copies the CURRENT Steeltoe.Management.GitProperties.Build source into this repository and writes the default TestApp project referencing it - see
    /// <see cref="TestProjectWriter.CopyCurrentProjectFilesAsync" />. Deliberately does not commit anything: this is meant for the "fully custom setup"
    /// scenario where any commit is already the caller's own responsibility.
    /// </summary>
    public async Task<GitRepository> AddTestAppAsync()
    {
        TestProject testApp = await GitRepository.WriteDefaultTestAppAsync(RootDirectory);
        return new GitRepository(workspace, RootDirectory, testApp);
    }
}
