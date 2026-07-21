// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class WriteGitPropertiesFallbackFileThenSimulatedPushServerPublishUsesItTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 2, true);
        await repository.TestApp.BuildAsync("-t:WriteGitPropertiesFallbackFile");
        Dictionary<string, string> fallbackProperties = await repository.TestApp.ReadFallbackPropertiesAsync();

        RemotePushProjectTree remote = repository.SimulatePush("pushed");
        remote.TestApp.FallbackGitPropertiesGenerated.Should().BeTrue();

        DotNetCommandOutput output = await remote.TestApp.PublishAsync("-v:normal");
        output.Value.Should().Contain("using pre-generated fallback file");

        Dictionary<string, string> publishProperties = await remote.TestApp.ReadReleasePublishPropertiesAsync();
        publishProperties.Should().BeEquivalentTo(fallbackProperties);
    }
}
