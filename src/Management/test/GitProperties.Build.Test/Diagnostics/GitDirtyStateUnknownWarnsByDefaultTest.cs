// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.Diagnostics;

public sealed class GitDirtyStateUnknownWarnsByDefaultTest : GitPropertiesBuildTestBase
{
    private const string BogusGitExecutable = "this-executable-definitely-does-not-exist-anywhere";

    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        await repository.TestApp.BuildAsync();
        repository.SharedCacheExists.Should().BeTrue();

        DotNetCommandOutput defaultOutput = await repository.TestApp.BuildAsync($"-p:GitExecutable={BogusGitExecutable}");
        defaultOutput.Should().ContainOnlyGitWarning(GitDiagnostic.GitDirtyStateUnknown);
        repository.TestApp.GitPropertiesGenerated.Should().BeTrue();

        Dictionary<string, string> properties = await repository.TestApp.ReadDebugPropertiesAsync();
        properties.Should().NotContainKey("git.dirty");
        string expectedCommitId = await repository.GetCommitIdAsync();
        properties["git.commit.id"].Should().Be(expectedCommitId);

        DotNetCommandOutput infoOutput = await repository.TestApp.BuildAsync($"-p:GitExecutable={BogusGitExecutable}", "-p:GitPropertiesEnableWarnings=false");
        infoOutput.Should().ContainOnlyGitMessage(GitDiagnostic.GitDirtyStateUnknown);
        repository.TestApp.GitPropertiesGenerated.Should().BeTrue();

        DotNetCommandOutput disabledOutput = await repository.TestApp.BuildAsync($"-p:GitExecutable={BogusGitExecutable}", "-p:GenerateGitProperties=false");
        disabledOutput.Should().NotContainAnyGitWarnings();
        repository.TestApp.GitPropertiesGenerated.Should().BeFalse();
    }
}
