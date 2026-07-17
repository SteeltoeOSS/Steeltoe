// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class BuildTimeChangesAcrossBuildsUnlikeCommitTimeTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// Guards against git.build.time accidentally ending up in the shared, cross-project/cross-TFM cache (see GenerateGitPropertiesCacheTask) instead of
    /// being recomputed by ComposeGitPropertiesTask on every build - same class of regression IncrementalBuildCacheSkipsButDirtyStaysLiveTest guards against
    /// for git.dirty. A cached build time would go stale (reporting the FIRST build's time on every subsequent one), silently defeating the whole point of
    /// the field: telling you when THIS build actually ran.
    /// </summary>
    [Fact]
    public async Task BuildTime_ChangesAcrossBuilds_UnlikeCommitTime()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        await ProcessRunner.RunDotnetAsync(testApp, "build");
        Dictionary<string, string> propertiesBefore = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));

        // git.build.time is formatted "yyyy-MM-ddTHH:mm:sszzz" (ComposeGitPropertiesTask) - second
        // resolution only, no fractional part - so two builds landing within the same wall-clock
        // second would produce identical values no matter how this test is written. This delay is
        // sized to that format's own precision, not incidental slack.
        await Task.Delay(TimeSpan.FromMilliseconds(1100), TestContext.Current.CancellationToken);

        await ProcessRunner.RunDotnetAsync(testApp, "build");
        Dictionary<string, string> propertiesAfter = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));

        propertiesAfter["git.build.time"].Should().NotBe(propertiesBefore["git.build.time"],
            "git.build.time must be recomputed on every build, not reused from the shared cache.");

        propertiesAfter["git.commit.time"].Should().Be(propertiesBefore["git.commit.time"],
            "git.commit.time must stay tied to the (unchanged) commit, unlike git.build.time.");

        propertiesAfter["git.commit.id"].Should().Be(propertiesBefore["git.commit.id"], "nothing about the commit itself changed between the two builds.");
    }
}
