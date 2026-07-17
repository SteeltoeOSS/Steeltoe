// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class MultiTargetedProjectSharesCacheAcrossTargetFrameworksTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// A single multi-targeted project (current TFM plus the one immediately before it - see <see cref="TestPaths.MultiTargetTestFrameworks" />) is a
    /// different sharing scenario than <see cref="MultiProjectSharesCacheTest" />: MSBuild builds a multi-targeted project's inner TFMs concurrently by
    /// default (unlike the two sequential "dotnet build" invocations that test uses), which is exactly the race
    /// GenerateGitPropertiesCacheTask.TryGenerateAndWriteCache's cross-process lock exists to handle. Also guards against the regression that fix's first
    /// (wrong) attempt introduced: tagging the current commit invalidates the cache without changing the commit ID, so a naive "does the cache already
    /// reflect this commit" freshness check would wrongly skip regenerating it.
    /// </summary>
    [Fact]
    public async Task MultiTargetedProject_SharesCacheAcrossTargetFrameworks()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1);
        string testApp = await GitPropertiesTestWorkspace.WriteAppProjectAsync(repository, "MultiTargetApp", TestPaths.MultiTargetTestFrameworks);
        string[] frameworks = TestPaths.MultiTargetTestFrameworks.Split(';');

        await ProcessRunner.RunDotnetAsync(testApp, "build");

        string expectedCommitId = await ProcessRunner.GetGitOutputAsync(repository, "rev-parse", "HEAD");
        List<Dictionary<string, string>> propertiesBefore = await GetGitPropertiesPerTargetFrameworkAsync(testApp, frameworks);

        foreach (Dictionary<string, string> properties in propertiesBefore)
        {
            properties["git.commit.id"].Should().Be(expectedCommitId);
            properties["git.tags"].Should().BeEmpty();
        }

        await ProcessRunner.RunGitAsync(repository, "tag", "v1.0.0");

        await ProcessRunner.RunDotnetAsync(testApp, "build");

        List<Dictionary<string, string>> propertiesAfter = await GetGitPropertiesPerTargetFrameworkAsync(testApp, frameworks);

        foreach (Dictionary<string, string> properties in propertiesAfter)
        {
            properties["git.tags"].Should().Be("v1.0.0", "both target frameworks must observe the new tag, even though the commit it points at didn't change.");
        }
    }
}
