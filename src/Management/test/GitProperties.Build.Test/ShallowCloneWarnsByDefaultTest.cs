// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class ShallowCloneWarnsByDefaultTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository source = await Workspace.CreateGitRepositoryAsync("source", 1);
        GitRepository shallow = await source.CloneAsShallowAsync("shallow");

        DotNetCommandOutput defaultOutput = await shallow.TestApp.BuildAsync("-v:normal");
        defaultOutput.Should().ContainGitWarning(GitDiagnosticId.GitRepositoryIsShallowClone);

        shallow.DeleteSharedCache();
        DotNetCommandOutput disableWarningsOutput = await shallow.TestApp.BuildAsync("-p:GitPropertiesEnableWarnings=false", "-v:normal");
        disableWarningsOutput.Should().ContainGitInfo(GitDiagnosticId.GitRepositoryIsShallowClone, "repository is a shallow clone");

        shallow.DeleteSharedCache();
        DotNetCommandOutput featureOffOutput = await shallow.TestApp.BuildAsync("-p:GenerateGitProperties=false");
        featureOffOutput.Should().NotContainGitWarning(GitDiagnosticId.GitRepositoryIsShallowClone);

        shallow.TestApp.GitPropertiesGenerated.Should().BeTrue();
    }
}
