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
        defaultOutput.Should().ContainGitWarning(GitDiagnosticId.GitDirtyStateUnknown, "failed (");
        repository.TestApp.GitPropertiesGenerated.Should().BeTrue();

        Dictionary<string, string> properties = await repository.TestApp.ReadDebugPropertiesAsync();
        properties.Should().NotContainKey("git.dirty");
        string expectedCommitId = await repository.GetCommitIdAsync();
        properties["git.commit.id"].Should().Be(expectedCommitId);

        DotNetCommandOutput disableWarningsOutput =
            await repository.TestApp.BuildAsync($"-p:GitExecutable={BogusGitExecutable}", "-p:GitPropertiesEnableWarnings=false");

        disableWarningsOutput.Should().ContainGitMessage(GitDiagnosticId.GitDirtyStateUnknown, "failed (");
        repository.TestApp.GitPropertiesGenerated.Should().BeTrue();

        DotNetCommandOutput featureOffOutput = await repository.TestApp.BuildAsync($"-p:GitExecutable={BogusGitExecutable}", "-p:GenerateGitProperties=false");
        featureOffOutput.Should().NotContainGitWarning(GitDiagnosticId.GitDirtyStateUnknown);
        repository.TestApp.GitPropertiesGenerated.Should().BeFalse();

        string nonZeroExitCodeGitExecutable = await GitPropertiesTestWorkspace.GetNonZeroExitCodeGitExecutableAsync();
        DotNetCommandOutput nonZeroExitCodeOutput = await repository.TestApp.BuildAsync($"-p:GitExecutable={nonZeroExitCodeGitExecutable}");
        nonZeroExitCodeOutput.Should().ContainGitWarning(GitDiagnosticId.GitDirtyStateUnknown, "exited with code");
        repository.TestApp.GitPropertiesGenerated.Should().BeTrue();

        Dictionary<string, string> nonZeroExitCodeProperties = await repository.TestApp.ReadDebugPropertiesAsync();
        nonZeroExitCodeProperties.Should().NotContainKey("git.dirty");
    }
}
