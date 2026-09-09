// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.FallbackFile;

public sealed class FallbackFileWithoutGitignoreMakesLaterBuildsAppearDirtyTest : GitPropertiesTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        await repository.TestApp.BuildAsync("-p:GitPropertiesWriteToProjectDirectory=true");
        bool isDirty = await repository.IsDirtyAsync();
        isDirty.Should().BeTrue();

        await repository.TestApp.BuildAsync("-p:GitPropertiesWriteToProjectDirectory=true");
        Dictionary<string, string> properties = await repository.TestApp.ReadDebugPropertiesAsync();
        properties["git.dirty"].Should().Be("true");
    }
}
