// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class NoGitRepositoryWarnsByDefaultTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        TestProject testApp = await Workspace.CreateProjectWithoutGitAsync("test-project");

        DotNetCommandOutput defaultOutput = await testApp.BuildAsync();
        defaultOutput.Should().ContainGitWarning(GitDiagnosticId.GitRepositoryNotFound);
        testApp.GitPropertiesGenerated.Should().BeFalse();

        DotNetCommandOutput disableWarningsOutput = await testApp.BuildAsync("-p:GitPropertiesEnableWarnings=false");
        disableWarningsOutput.Should().ContainGitMessage(GitDiagnosticId.GitRepositoryNotFound);
        testApp.GitPropertiesGenerated.Should().BeFalse();

        DotNetCommandOutput featureOffOutput = await testApp.BuildAsync("-p:GenerateGitProperties=false");
        featureOffOutput.Should().NotContainGitWarning(GitDiagnosticId.GitRepositoryNotFound);

        testApp.GitPropertiesGenerated.Should().BeFalse();
    }
}
