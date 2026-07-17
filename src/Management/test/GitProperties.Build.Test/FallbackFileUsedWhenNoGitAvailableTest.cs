// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class FallbackFileUsedWhenNoGitAvailableTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// End-to-end simulation of the scenario that motivated $(GitPropertiesWriteToProjectDirectory) in the first place: `cf push` using the
    /// dotnet_core_buildpack directly from source, which strips ".git" from the pushed tree unconditionally (see SimulateSourcePush) - meaning live
    /// generation can never run for that push, ever. A pre-generated fallback file (produced by an earlier LOCAL build, where .git was available) must ride
    /// along in the pushed source tree and get picked up, ending up in the published output exactly as if it had been generated live.
    /// </summary>
    [Fact]
    public async Task FallbackFile_UsedWhenNoGitAvailable()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 2, true);
        await repository.TestApp.BuildAsync("-p:GitPropertiesWriteToProjectDirectory=true");
        Dictionary<string, string> fallbackProperties = await repository.TestApp.ReadFallbackPropertiesAsync();
        fallbackProperties["git.dirty"].Should().Be("false", "the gitignored fallback file must not make its own producing build see the tree as dirty.");

        RemotePushProjectTree remote = repository.SimulatePush("pushed");
        remote.HasGitDirectory.Should().BeFalse("the simulated push must not carry '.git' along, matching cf push's own default.");
        remote.TestApp.FallbackGitPropertiesGenerated.Should().BeTrue("the fallback git.properties must have survived the simulated push.");

        string publishResult = await remote.TestApp.PublishAsync("-v:detailed");
        publishResult.Should().NotContain("GITPROPS001", "the fallback file should suppress the usual no-.git diagnostic entirely.");
        publishResult.Should().Contain("using pre-generated fallback file", "using the fallback should still be traceable, so it's never silently stale.");

        Dictionary<string, string> publishedProperties = await remote.TestApp.ReadReleasePublishPropertiesAsync();
        publishedProperties.Should().BeEquivalentTo(fallbackProperties, "the fallback-produced output must exactly match the pre-generated fallback content.");
    }
}
