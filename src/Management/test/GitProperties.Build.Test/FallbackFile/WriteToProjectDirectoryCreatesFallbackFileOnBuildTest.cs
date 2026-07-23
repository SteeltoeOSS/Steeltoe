// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.FallbackFile;

public sealed class WriteToProjectDirectoryCreatesFallbackFileOnBuildTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1, true);
        DotNetCommandOutput output = await repository.TestApp.BuildAsync("-p:GitPropertiesWriteToProjectDirectory=true");
        output.Value.Should().Contain($"git.properties: writing fallback copy to '{repository.TestApp.FallbackFilePath}'.");
        repository.TestApp.FallbackGitPropertiesGenerated.Should().BeTrue();

        Dictionary<string, string> fallbackProperties = await repository.TestApp.ReadFallbackPropertiesAsync();
        Dictionary<string, string> outputProperties1 = await repository.TestApp.ReadDebugPropertiesAsync();
        fallbackProperties.Should().BeEquivalentTo(outputProperties1);

        bool isDirty = await repository.IsDirtyAsync();
        isDirty.Should().BeFalse();

        await repository.TestApp.BuildAsync("-p:GitPropertiesWriteToProjectDirectory=true");
        Dictionary<string, string> outputProperties2 = await repository.TestApp.ReadDebugPropertiesAsync();
        outputProperties2["git.dirty"].Should().Be("false");
    }
}
