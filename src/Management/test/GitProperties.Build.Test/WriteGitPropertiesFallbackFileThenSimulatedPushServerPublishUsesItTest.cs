// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class WriteGitPropertiesFallbackFileThenSimulatedPushServerPublishUsesItTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// End-to-end simulation of the actual documented workflow - the lightweight-target equivalent of <see cref="FallbackFileUsedWhenNoGitAvailableTest" />
    /// (which uses a full build instead): produce the fallback file via
    /// <c>
    /// WriteGitPropertiesFallbackFile
    /// </c>
    /// , simulate a source-based `cf push`, and confirm the server-side publish still picks it up correctly.
    /// </summary>
    [Fact]
    public async Task WriteGitPropertiesFallbackFile_ThenSimulatedPush_ServerPublishUsesIt()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 2, true);
        await repository.TestApp.BuildAsync("-t:WriteGitPropertiesFallbackFile");
        Dictionary<string, string> fallbackProperties = await repository.TestApp.ReadFallbackPropertiesAsync();

        RemotePushProjectTree remote = repository.SimulatePush("pushed");
        remote.TestApp.FallbackGitPropertiesGenerated.Should().BeTrue("the fallback git.properties must have survived the simulated push.");

        string publishResult = await remote.TestApp.PublishAsync("-v:detailed");
        publishResult.Should().Contain("using pre-generated fallback file", "using the fallback should still be traceable, so it's never silently stale.");

        Dictionary<string, string> publishedProperties = await remote.TestApp.ReadReleasePublishPropertiesAsync();
        publishedProperties.Should().BeEquivalentTo(fallbackProperties, "the fallback-produced output must exactly match the pre-generated fallback content.");
    }
}
