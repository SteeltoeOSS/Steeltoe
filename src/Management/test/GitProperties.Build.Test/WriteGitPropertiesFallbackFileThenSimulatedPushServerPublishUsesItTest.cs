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
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 2, true);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        await ProcessRunner.RunDotnetAsync(testApp, "build", "-t:WriteGitPropertiesFallbackFile");
        Dictionary<string, string> fallbackProperties = await PropertiesFile.ReadAsync(GetFallbackFilePath(testApp));

        string destinationDirectory = Path.Combine(Workspace.RootDirectory, "pushed");
        string pushedRoot = SyntheticGitRepositoryBuilder.SimulateSourcePush(repository, destinationDirectory);
        string pushedApp = Path.Combine(pushedRoot, GitPropertiesTestWorkspace.TestAppProjectName);
        File.Exists(GetFallbackFilePath(pushedApp)).Should().BeTrue("the fallback git.properties must have survived the simulated push.");

        string publishResult = await ProcessRunner.RunDotnetAsync(pushedApp, "publish", "-v:detailed");
        publishResult.Should().Contain("using pre-generated fallback file", "using the fallback should still be traceable, so it's never silently stale.");

        Dictionary<string, string> publishedProperties = await PropertiesFile.ReadAsync(GetReleasePublishGitPropertiesFilePath(pushedApp));
        publishedProperties.Should().BeEquivalentTo(fallbackProperties, "the fallback-produced output must exactly match the pre-generated fallback content.");
    }
}
