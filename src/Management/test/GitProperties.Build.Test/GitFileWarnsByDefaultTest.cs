// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class GitFileWarnsByDefaultTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task GitFile_WarnsByDefault()
    {
        string projectDirectory = Workspace.GetPath("test-project");
        TestProject testApp = await Workspace.CreateProjectDirectoryAsync("test-project");

        // ".git" must sit above BOTH TestApp and Steeltoe.Management.GitProperties.Build for the repo-root walk
        // (which starts at TestApp, the project actually being built) to find it - i.e. at
        // projectDirectory itself.
        await File.WriteAllTextAsync(Path.Combine(projectDirectory, ".git"), "gitdir: /some/where/.git/worktrees/test-project",
            TestContext.Current.CancellationToken);

        string defaultResult = await testApp.BuildAsync();
        defaultResult.AssertWarned("GITPROPS002");

        string enableWarningsFalseResult = await testApp.BuildAsync("-p:GitPropertiesEnableWarnings=false", "-v:normal");
        enableWarningsFalseResult.AssertReportedAsInfoOnly("GITPROPS002", "resolves to a git worktree or submodule");

        string featureOffResult = await testApp.BuildAsync("-p:GenerateGitProperties=false");
        featureOffResult.Should().NotContain("GITPROPS002");

        testApp.GitPropertiesGenerated.Should().BeFalse();
    }
}
