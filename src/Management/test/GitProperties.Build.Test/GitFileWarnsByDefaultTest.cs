// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class GitFileWarnsByDefaultTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task GitFile_WarnsByDefault()
    {
        string projectDirectory = Path.Combine(Workspace.RootDirectory, "proj");
        Directory.CreateDirectory(projectDirectory);
        string testApp = await Workspace.CopyCurrentProjectFilesAsync(projectDirectory);
        // ".git" must sit above BOTH TestApp and Steeltoe.Management.GitProperties.Build for the repo-root walk
        // (which starts at TestApp, the project actually being built) to find it - i.e. at
        // projectDirectory itself.
        await File.WriteAllTextAsync(Path.Combine(projectDirectory, ".git"), "gitdir: /some/where/.git/worktrees/proj", TestContext.Current.CancellationToken);

        string defaultResult = await ProcessRunner.RunDotnetAsync(testApp, "build");
        AssertWarned(defaultResult, "GITPROPS002");
        AssertNoGitPropertiesGenerated(testApp);

        string enableWarningsFalseResult = await ProcessRunner.RunDotnetAsync(testApp, "build", "-p:GitPropertiesEnableWarnings=false", "-v:normal");
        AssertReportedAsInfoOnly(enableWarningsFalseResult, "GITPROPS002", "resolves to a git worktree or submodule");

        string featureOffResult = await ProcessRunner.RunDotnetAsync(testApp, "build", "-p:GenerateGitProperties=false");
        featureOffResult.Should().NotContain("GITPROPS002");
    }
}
