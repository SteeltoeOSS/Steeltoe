// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.SharedCache;

public sealed class IncrementalBuildCacheSkipsButDirtyStaysLiveTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        await repository.TestApp.BuildAsync();
        repository.SharedCacheExists.Should().BeTrue();

        Dictionary<string, string> propertiesBefore = await repository.TestApp.ReadDebugPropertiesAsync();
        propertiesBefore["git.dirty"].Should().Be("false");

        await Workspace.WriteFileAsync(Path.Combine(repository.TestApp.RootDirectory, "uncommitted.txt"), "uncommitted content");
        DotNetCommandOutput output = await repository.TestApp.BuildAsync("-v:normal");
        output.Value.Should().Contain("Skipping target \"GenerateGitPropertiesCache\"");

        Dictionary<string, string> propertiesAfter = await repository.TestApp.ReadDebugPropertiesAsync();
        propertiesAfter["git.dirty"].Should().Be("true");
    }
}
