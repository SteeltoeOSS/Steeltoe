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
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 2, true);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult fallbackResult = await ProcessRunner.RunDotnetAsync(testApp, "build", "-p:GitPropertiesWriteToProjectDirectory=true");
        AssertBuildSucceeded(fallbackResult, "the local build that produces the fallback file");
        Dictionary<string, string> fallbackProperties = await PropertiesFile.ReadAsync(GetFallbackFilePath(testApp));
        fallbackProperties["git.dirty"].Should().Be("false", "the gitignored fallback file must not make its own producing build see the tree as dirty.");

        string pushedRoot = GitPropertiesTestWorkspace.SimulateSourcePush(repository, Path.Combine(Workspace.RootDirectory, "pushed"));
        string pushedApp = Path.Combine(pushedRoot, GitPropertiesTestWorkspace.TestAppProjectName);
        Directory.Exists(Path.Combine(pushedRoot, ".git")).Should().BeFalse("the simulated push must not carry '.git' along, matching cf push's own default.");
        File.Exists(GetFallbackFilePath(pushedApp)).Should().BeTrue("the fallback git.properties must have survived the simulated push.");

        ProcessResult publishResult = await ProcessRunner.RunDotnetAsync(pushedApp, "publish", "-v:detailed");
        AssertBuildSucceeded(publishResult, "publish with no usable .git repository present");
        publishResult.Output.Should().NotContain("GITPROPS001", "the fallback file should suppress the usual no-.git diagnostic entirely.");

        publishResult.Output.Should().Contain(
            "using pre-generated fallback file", "using the fallback should still be traceable, so it's never silently stale.");

        Dictionary<string, string> publishedProperties = await PropertiesFile.ReadAsync(GetReleasePublishGitPropertiesFilePath(pushedApp));
        publishedProperties.Should().BeEquivalentTo(fallbackProperties, "the fallback-produced output must exactly match the pre-generated fallback content.");
    }
}
