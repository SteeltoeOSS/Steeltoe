// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class IncompatibleGitVersionWarnsByDefaultTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        string fakeGitExecutable = await Workspace.CreateFakeGitExecutableAsync("git version 2.14.9");

        DotNetCommandOutput defaultOutput = await repository.TestApp.BuildAsync($"-p:GitExecutable={fakeGitExecutable}");
        defaultOutput.Should().ContainGitWarning(GitDiagnosticId.IncompatibleGitVersion);
        repository.TestApp.GitPropertiesGenerated.Should().BeFalse();

        DotNetCommandOutput disableWarningsOutput =
            await repository.TestApp.BuildAsync($"-p:GitExecutable={fakeGitExecutable}", "-p:GitPropertiesEnableWarnings=false");

        disableWarningsOutput.Should().ContainGitMessage(GitDiagnosticId.IncompatibleGitVersion);
        repository.TestApp.GitPropertiesGenerated.Should().BeFalse();

        DotNetCommandOutput featureOffOutput = await repository.TestApp.BuildAsync($"-p:GitExecutable={fakeGitExecutable}", "-p:GenerateGitProperties=false");
        featureOffOutput.Should().NotContainGitWarning(GitDiagnosticId.IncompatibleGitVersion);
        repository.TestApp.GitPropertiesGenerated.Should().BeFalse();
    }
}
