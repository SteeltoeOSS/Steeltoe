// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.Diagnostics;

public sealed class ShallowCloneWarnsByDefaultTest : GitPropertiesTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository source = await Workspace.CreateGitRepositoryAsync("source", 1);
        GitRepository shallow = await source.CloneAsShallowAsync("shallow");

        DotNetCommandOutput defaultOutput = await shallow.TestApp.BuildAsync();
        defaultOutput.Should().ContainOnlyGitWarning(GitDiagnostic.GitRepositoryIsShallowClone);
        shallow.TestApp.GitPropertiesGenerated.Should().BeTrue();

        shallow.DeleteSharedCache();
        DotNetCommandOutput infoOutput = await shallow.TestApp.BuildAsync("-p:GitPropertiesEnableWarnings=false");
        infoOutput.Should().ContainOnlyGitMessage(GitDiagnostic.GitRepositoryIsShallowClone);
        shallow.TestApp.GitPropertiesGenerated.Should().BeTrue();

        shallow.DeleteSharedCache();
        DotNetCommandOutput disabledOutput = await shallow.TestApp.BuildAsync("-p:GenerateGitProperties=false");
        disabledOutput.Should().NotContainAnyGitWarnings();
        shallow.TestApp.GitPropertiesGenerated.Should().BeFalse();
    }
}
