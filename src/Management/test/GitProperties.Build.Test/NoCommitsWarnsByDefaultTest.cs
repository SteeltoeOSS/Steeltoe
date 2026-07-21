// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class NoCommitsWarnsByDefaultTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        EmptyGitRepository emptyRepository = await Workspace.CreateEmptyRepositoryAsync("repo");
        GitRepository repository = await emptyRepository.AddTestAppAsync();

        DotNetCommandOutput defaultOutput = await repository.TestApp.BuildAsync();
        defaultOutput.Should().ContainGitWarning(GitDiagnosticId.GitRepositoryHasNoCommits);

        DotNetCommandOutput disableWarningsOutput = await repository.TestApp.BuildAsync("-p:GitPropertiesEnableWarnings=false", "-v:normal");
        disableWarningsOutput.Should().ContainGitInfo(GitDiagnosticId.GitRepositoryHasNoCommits, "no commits yet");

        DotNetCommandOutput featureOffOutput = await repository.TestApp.BuildAsync("-p:GenerateGitProperties=false");
        featureOffOutput.Should().NotContainGitWarning(GitDiagnosticId.GitRepositoryHasNoCommits);

        repository.TestApp.GitPropertiesGenerated.Should().BeFalse();
    }
}
