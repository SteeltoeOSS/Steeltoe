// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class GitExecutableNotFoundWarnsByDefaultTest : GitPropertiesBuildTestBase
{
    private const string BogusGitExecutable = "this-executable-definitely-does-not-exist-anywhere";

    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        DotNetCommandOutput defaultOutput = await repository.TestApp.BuildAsync($"-p:GitExecutable={BogusGitExecutable}");
        defaultOutput.Should().ContainGitWarning(GitDiagnosticId.GitExecutableNotFound);

        DotNetCommandOutput disableWarningsOutput =
            await repository.TestApp.BuildAsync($"-p:GitExecutable={BogusGitExecutable}", "-p:GitPropertiesEnableWarnings=false", "-v:normal");

        disableWarningsOutput.Should().ContainGitInfo(GitDiagnosticId.GitExecutableNotFound, "git.properties generation skipped: could not run");

        DotNetCommandOutput featureOffOutput = await repository.TestApp.BuildAsync($"-p:GitExecutable={BogusGitExecutable}", "-p:GenerateGitProperties=false");
        featureOffOutput.Should().NotContainGitWarning(GitDiagnosticId.GitExecutableNotFound);

        repository.TestApp.GitPropertiesGenerated.Should().BeFalse();
    }
}
