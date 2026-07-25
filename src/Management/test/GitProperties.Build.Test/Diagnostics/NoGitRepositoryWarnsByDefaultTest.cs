// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.Diagnostics;

public sealed class NoGitRepositoryWarnsByDefaultTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        TestProject testApp = await Workspace.CreateProjectWithoutGitAsync("test-project");

        DotNetCommandOutput defaultOutput = await testApp.BuildAsync();
        defaultOutput.Should().ContainGitWarning(GitDiagnosticId.GitRepositoryNotFound, "no usable .git directory found");
        testApp.GitPropertiesGenerated.Should().BeFalse();

        DotNetCommandOutput disableWarningsOutput = await testApp.BuildAsync("-p:GitPropertiesEnableWarnings=false");
        disableWarningsOutput.Should().ContainGitMessage(GitDiagnosticId.GitRepositoryNotFound, "no usable .git directory found");
        testApp.GitPropertiesGenerated.Should().BeFalse();

        DotNetCommandOutput featureOffOutput = await testApp.BuildAsync("-p:GenerateGitProperties=false");
        featureOffOutput.Should().NotContainGitWarning(GitDiagnosticId.GitRepositoryNotFound);
        testApp.GitPropertiesGenerated.Should().BeFalse();

        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        string fakeGitExecutable = await Workspace.CreateFakeGitExecutableAsync("git version 2.15.0");
        DotNetCommandOutput notInsideWorkTreeOutput = await repository.TestApp.BuildAsync($"-p:GitExecutable={fakeGitExecutable}");
        notInsideWorkTreeOutput.Should().ContainGitWarning(GitDiagnosticId.GitRepositoryNotFound, "not inside a usable git repository");
        repository.TestApp.GitPropertiesGenerated.Should().BeFalse();
    }
}
