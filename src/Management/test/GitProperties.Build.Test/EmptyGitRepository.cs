// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

internal sealed class EmptyGitRepository(TestWorkspace workspace, string rootDirectory)
{
    public string RootDirectory { get; } = rootDirectory;

    public Task<string> RunGitAsync(params string[] arguments)
    {
        return ProcessRunner.RunGitAsync(RootDirectory, arguments);
    }

    public Task CommitAllAsync(string subject, string? body = null)
    {
        return GitRepositoryBuilder.CommitAllAsync(RootDirectory, subject, body);
    }

    public async Task<GitRepository> AddTestAppAsync()
    {
        // Deliberately does not commit anything: any commit is the caller's own responsibility.

        TestProject testApp = await workspace.WriteDefaultTestAppAsync(RootDirectory);
        return new GitRepository(workspace, RootDirectory, testApp);
    }
}
