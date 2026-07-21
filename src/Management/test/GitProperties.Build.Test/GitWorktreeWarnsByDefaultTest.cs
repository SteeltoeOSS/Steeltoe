// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class GitWorktreeWarnsByDefaultTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        string projectDirectory = Workspace.GetPath("test-project");
        TestProject testApp = await Workspace.CreateProjectWithoutGitAsync("test-project");
        await Workspace.WriteFileAsync(Path.Combine(projectDirectory, ".git"), "gitdir: /some/where/.git/worktrees/test-project");

        DotNetCommandOutput defaultOutput = await testApp.BuildAsync();
        defaultOutput.Should().ContainGitWarning(GitDiagnosticId.GitWorktreeFound);

        DotNetCommandOutput disableWarningsOutput = await testApp.BuildAsync("-p:GitPropertiesEnableWarnings=false", "-v:normal");
        disableWarningsOutput.Should().ContainGitInfo(GitDiagnosticId.GitWorktreeFound, "resolves to a git worktree or submodule");

        DotNetCommandOutput featureOffOutput = await testApp.BuildAsync("-p:GenerateGitProperties=false");
        featureOffOutput.Should().NotContainGitWarning(GitDiagnosticId.GitWorktreeFound);

        testApp.GitPropertiesGenerated.Should().BeFalse();
    }
}
