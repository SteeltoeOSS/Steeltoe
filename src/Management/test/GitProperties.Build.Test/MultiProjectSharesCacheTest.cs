// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class MultiProjectSharesCacheTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 2);
        TestProject projectA = await repository.AddProjectAsync("ProjectA");
        TestProject projectB = await repository.AddProjectAsync("ProjectB");
        DotNetCommandOutput outputA = await projectA.BuildAsync();
        outputA.Value.Should().Contain("git.properties: generating shared cache");
        repository.SharedCacheExists.Should().BeTrue();

        DotNetCommandOutput outputB = await projectB.BuildAsync();
        outputB.Value.Should().NotContain("git.properties: generating shared cache");

        Dictionary<string, string> propertiesA = await projectA.ReadDebugPropertiesAsync();
        Dictionary<string, string> propertiesB = await projectB.ReadDebugPropertiesAsync();
        propertiesB["git.commit.id"].Should().Be(propertiesA["git.commit.id"]);
    }
}
