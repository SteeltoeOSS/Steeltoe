// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.Diagnostics;

public sealed class NoCommitsWarnsByDefaultTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        EmptyGitRepository emptyRepository = await Workspace.CreateEmptyRepositoryAsync("repo");
        GitRepository repository = await emptyRepository.AddTestAppAsync();

        DotNetCommandOutput defaultOutput = await repository.TestApp.BuildAsync();
        defaultOutput.Should().ContainOnlyGitWarning(GitDiagnostic.GitRepositoryHasNoCommits);
        repository.TestApp.GitPropertiesGenerated.Should().BeFalse();

        DotNetCommandOutput infoOutput = await repository.TestApp.BuildAsync("-p:GitPropertiesEnableWarnings=false");
        infoOutput.Should().ContainOnlyGitMessage(GitDiagnostic.GitRepositoryHasNoCommits);
        repository.TestApp.GitPropertiesGenerated.Should().BeFalse();

        DotNetCommandOutput disabledOutput = await repository.TestApp.BuildAsync("-p:GenerateGitProperties=false");
        disabledOutput.Should().NotContainAnyGitWarnings();
        repository.TestApp.GitPropertiesGenerated.Should().BeFalse();
    }
}
