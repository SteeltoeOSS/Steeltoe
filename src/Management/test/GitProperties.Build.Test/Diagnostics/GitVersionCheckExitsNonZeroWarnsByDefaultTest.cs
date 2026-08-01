// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.Diagnostics;

public sealed class GitVersionCheckExitsNonZeroWarnsByDefaultTest : GitPropertiesTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        string gitExecutable = await TestWorkspace.GetNonZeroExitCodeGitExecutableAsync();

        DotNetCommandOutput defaultOutput = await repository.TestApp.BuildAsync($"-p:GitExecutable={gitExecutable}");
        defaultOutput.Should().ContainOnlyGitWarning(GitDiagnostic.GitVersionCheckExitsNonZero);
        repository.TestApp.GitPropertiesGenerated.Should().BeFalse();

        DotNetCommandOutput infoOutput = await repository.TestApp.BuildAsync($"-p:GitExecutable={gitExecutable}", "-p:GitPropertiesEnableWarnings=false");
        infoOutput.Should().ContainOnlyGitMessage(GitDiagnostic.GitVersionCheckExitsNonZero);
        repository.TestApp.GitPropertiesGenerated.Should().BeFalse();

        DotNetCommandOutput disabledOutput = await repository.TestApp.BuildAsync($"-p:GitExecutable={gitExecutable}", "-p:GenerateGitProperties=false");
        disabledOutput.Should().NotContainAnyGitWarnings();
        repository.TestApp.GitPropertiesGenerated.Should().BeFalse();
    }
}
