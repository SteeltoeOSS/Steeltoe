// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.FallbackFile;

public sealed class WriteToProjectDirectoryCreatesFallbackFileOnPublishTest : GitPropertiesTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1, true);

        DotNetCommandOutput output =
            await repository.TestApp.PublishAsync("-p:GitPropertiesWriteToProjectDirectory=true", "-p:GitPropertiesEnableWarnings=true");

        output.Should().NotContainAnyGitWarnings();
        repository.TestApp.FallbackGitPropertiesGenerated.Should().BeTrue();

        Dictionary<string, string> fallbackProperties = await repository.TestApp.ReadFallbackPropertiesAsync();
        Dictionary<string, string> publishProperties = await repository.TestApp.ReadReleasePublishPropertiesAsync();
        fallbackProperties.Should().BeEquivalentTo(publishProperties);

        bool isDirty = await repository.IsDirtyAsync();
        isDirty.Should().BeFalse();
    }
}
