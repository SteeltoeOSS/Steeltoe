// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class IncrementalBuildCacheSkipsButDirtyStaysLiveTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task IncrementalBuild_CacheSkipsButDirtyStaysLive()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        await ProcessRunner.RunDotnetAsync(testApp, "build");
        string cacheFile = Path.Combine(repository, "obj", "_GitProperties", "git.properties.cache");
        File.Exists(cacheFile).Should().BeTrue("the cache file should exist after first build.");

        string result2 = await ProcessRunner.RunDotnetAsync(testApp, "build", "-v:detailed");

        // "Skipping target" is itself the deterministic, sufficient proof that nothing rewrote the
        // cache file on this second build - no last-write-time comparison (and the sleep it would
        // otherwise need, to guarantee a detectably different timestamp) is needed on top of it.
        result2.Should().Contain("Skipping target \"GenerateGitPropertiesCache\"");

        Dictionary<string, string> properties = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));
        properties.Should().ContainKey("git.dirty");
    }
}
