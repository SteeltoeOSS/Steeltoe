// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.SharedCache;

public sealed class MultiProjectSharesCacheTest : GitPropertiesTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 2);
        DotNetCommandOutput defaultAppOutput = await repository.TestApp.BuildAsync();
        defaultAppOutput.Value.Should().Contain("git.properties: generating shared cache");
        repository.SharedCacheExists.Should().BeTrue();

        TestProject extraApp = await repository.AddTestAppAsync("ExtraApp", [
            Workspace.GitPropertiesPackageReference,
            Workspace.FakeEndpointPackageReference
        ]);

        DotNetCommandOutput extraAppOutput = await extraApp.BuildAsync();
        extraAppOutput.Value.Should().NotContain("git.properties: generating shared cache");

        Dictionary<string, string> testAppProperties = await repository.TestApp.ReadDebugPropertiesAsync();
        Dictionary<string, string> extraAppProperties = await extraApp.ReadDebugPropertiesAsync();
        extraAppProperties["git.commit.id"].Should().Be(testAppProperties["git.commit.id"]);
    }
}
