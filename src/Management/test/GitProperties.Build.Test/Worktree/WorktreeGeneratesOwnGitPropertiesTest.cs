// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.Worktree;

public sealed class WorktreeGeneratesOwnGitPropertiesTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository main = await Workspace.CreateGitRepositoryAsync("main", 1);
        await main.TestApp.BuildAsync();
        Dictionary<string, string> mainPropertiesBefore = await main.TestApp.ReadDebugPropertiesAsync();

        GitRepository side = await main.AddWorktreeAsync("side", "feature");
        DotNetCommandOutput sideOutput = await side.TestApp.BuildAsync();
        sideOutput.Should().NotContainAnyGitWarnings();
        side.TestApp.GitPropertiesGenerated.Should().BeTrue();

        Dictionary<string, string> sidePropertiesBeforeCommit = await side.TestApp.ReadDebugPropertiesAsync();
        sidePropertiesBeforeCommit["git.branch"].Should().Be("feature");
        sidePropertiesBeforeCommit["git.commit.id"].Should().Be(mainPropertiesBefore["git.commit.id"]);

        await side.RunGitAsync("commit", "--quiet", "--allow-empty", "-m", "Worktree-only commit");
        await side.TagAsync("from-side");
        await side.TestApp.BuildAsync();
        Dictionary<string, string> sidePropertiesAfterCommit = await side.TestApp.ReadDebugPropertiesAsync();
        sidePropertiesAfterCommit["git.commit.id"].Should().NotBe(mainPropertiesBefore["git.commit.id"]);
        sidePropertiesAfterCommit["git.tags"].Should().Be("from-side");

        DotNetCommandOutput mainOutputAfter = await main.TestApp.BuildAsync();
        mainOutputAfter.Should().NotContainAnyGitWarnings();
        Dictionary<string, string> mainPropertiesAfter = await main.TestApp.ReadDebugPropertiesAsync();
        mainPropertiesAfter["git.branch"].Should().Be(mainPropertiesBefore["git.branch"]);
        mainPropertiesAfter["git.commit.id"].Should().Be(mainPropertiesBefore["git.commit.id"]);

        string tagsVisibleFromMain = await main.RunGitAsync("tag");
        tagsVisibleFromMain.Should().Contain("from-side");

        main.SharedCacheExists.Should().BeTrue();
        side.SharedCacheExists.Should().BeTrue();
    }
}
