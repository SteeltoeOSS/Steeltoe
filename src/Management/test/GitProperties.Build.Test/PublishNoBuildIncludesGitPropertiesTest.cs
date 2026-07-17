// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class PublishNoBuildIncludesGitPropertiesTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Publish_NoBuild_IncludesGitProperties()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        await ProcessRunner.RunDotnetAsync(testApp, "build", "-c", "Release");

        string publishResult = await ProcessRunner.RunDotnetAsync(testApp, "publish", "-c", "Release", "--no-build");
        publishResult.Should().NotContain("duplicate");

        Dictionary<string, string> properties = await PropertiesFile.ReadAsync(GetReleasePublishGitPropertiesFilePath(testApp));
        string expectedCommitId = await ProcessRunner.GetGitOutputAsync(repository, "rev-parse", "HEAD");
        properties["git.commit.id"].Should().Be(expectedCommitId);
    }
}
