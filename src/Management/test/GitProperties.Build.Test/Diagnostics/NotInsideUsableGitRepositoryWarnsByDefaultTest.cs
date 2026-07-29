// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.Diagnostics;

public sealed class NotInsideUsableGitRepositoryWarnsByDefaultTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        string fakeGitExecutable = await Workspace.CreateFakeGitExecutableAsync("git version 2.15.0");

        DotNetCommandOutput defaultOutput = await repository.TestApp.BuildAsync($"-p:GitExecutable={fakeGitExecutable}");
        defaultOutput.Should().ContainOnlyGitWarning(GitDiagnostic.NotInsideUsableGitRepository);
        repository.TestApp.GitPropertiesGenerated.Should().BeFalse();

        DotNetCommandOutput infoOutput = await repository.TestApp.BuildAsync($"-p:GitExecutable={fakeGitExecutable}", "-p:GitPropertiesEnableWarnings=false");
        infoOutput.Should().ContainOnlyGitMessage(GitDiagnostic.NotInsideUsableGitRepository);
        repository.TestApp.GitPropertiesGenerated.Should().BeFalse();

        DotNetCommandOutput disabledOutput = await repository.TestApp.BuildAsync($"-p:GitExecutable={fakeGitExecutable}", "-p:GenerateGitProperties=false");
        disabledOutput.Should().NotContainAnyGitWarnings();
        repository.TestApp.GitPropertiesGenerated.Should().BeFalse();
    }
}
