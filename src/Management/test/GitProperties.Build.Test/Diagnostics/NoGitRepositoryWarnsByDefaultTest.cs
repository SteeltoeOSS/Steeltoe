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
        defaultOutput.Should().ContainOnlyGitWarning(GitDiagnostic.GitRepositoryNotFound);
        testApp.GitPropertiesGenerated.Should().BeFalse();

        DotNetCommandOutput infoOutput = await testApp.BuildAsync("-p:GitPropertiesEnableWarnings=false");
        infoOutput.Should().ContainOnlyGitMessage(GitDiagnostic.GitRepositoryNotFound);
        testApp.GitPropertiesGenerated.Should().BeFalse();

        DotNetCommandOutput disabledOutput = await testApp.BuildAsync("-p:GenerateGitProperties=false");
        disabledOutput.Should().NotContainAnyGitWarnings();
        testApp.GitPropertiesGenerated.Should().BeFalse();
    }
}
