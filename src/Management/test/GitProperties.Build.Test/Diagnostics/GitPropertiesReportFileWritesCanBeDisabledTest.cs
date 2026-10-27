// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.Diagnostics;

public sealed class GitPropertiesReportFileWritesCanBeDisabledTest : GitPropertiesTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1, true);

        DotNetCommandOutput defaultOutput = await repository.TestApp.BuildAsync("-p:GitPropertiesWriteToProjectDirectory=true");
        defaultOutput.Value.Should().Contain("git.properties: generating shared cache");
        defaultOutput.Value.Should().Contain("git.properties: writing to");
        defaultOutput.Value.Should().Contain("git.properties: writing fallback copy to");

        repository.DeleteSharedCache();

        // Remove the outputs of the first build, otherwise the assertions below are satisfied by files that were not written by the silent build.
        repository.TestApp.DeleteGeneratedGitProperties();
        repository.TestApp.GitPropertiesGenerated.Should().BeFalse();
        repository.TestApp.FallbackGitPropertiesGenerated.Should().BeFalse();

        DotNetCommandOutput silentOutput =
            await repository.TestApp.BuildAsync("-p:GitPropertiesWriteToProjectDirectory=true", "-p:GitPropertiesReportFileWrites=false");

        silentOutput.Value.Should().NotContain("git.properties: generating shared cache");
        silentOutput.Value.Should().NotContain("git.properties: writing to");
        silentOutput.Value.Should().NotContain("git.properties: writing fallback copy to");

        repository.TestApp.GitPropertiesGenerated.Should().BeTrue();
        repository.TestApp.FallbackGitPropertiesGenerated.Should().BeTrue();
    }
}
