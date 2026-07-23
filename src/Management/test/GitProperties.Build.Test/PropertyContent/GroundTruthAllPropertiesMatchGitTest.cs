// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace Steeltoe.Management.GitProperties.Build.Test.PropertyContent;

public sealed class GroundTruthAllPropertiesMatchGitTest : GitPropertiesBuildTestBase
{
    // Also piggybacks two unrelated checks on this same build rather than paying for another subprocess: that the fallback file is never written
    // unless explicitly opted into, and that writing git.properties is confirmed at default verbosity.
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 3);
        DotNetCommandOutput output = await repository.TestApp.BuildAsync();

        repository.TestApp.FallbackGitPropertiesGenerated.Should().BeFalse();

        string expectedPath = Path.Combine(repository.TestApp.RootDirectory, "obj", "Debug", TestPaths.TestAppTargetFramework, "git.properties");
        output.Value.Should().Contain($"git.properties: writing to '{expectedPath}'.");

        Dictionary<string, string> properties = await repository.TestApp.ReadDebugPropertiesAsync();

        string expectedCommitId = await repository.GetCommitIdAsync();
        properties["git.commit.id"].Should().Be(expectedCommitId);

        string expectedCommitIdAbbrev = await repository.RunGitAsync("rev-parse", "--short=7", "HEAD");
        properties["git.commit.id.abbrev"].Should().Be(expectedCommitIdAbbrev);

        string expectedCommitUserName = await repository.RunGitAsync("log", "-1", "--format=%an");
        properties["git.commit.user.name"].Should().Be(expectedCommitUserName);

        string expectedCommitUserEmail = await repository.RunGitAsync("log", "-1", "--format=%ae");
        properties["git.commit.user.email"].Should().Be(expectedCommitUserEmail);

        string expectedCommitMessageShort = await repository.RunGitAsync("log", "-1", "--format=%s");
        properties["git.commit.message.short"].Should().Be(expectedCommitMessageShort);

        string expectedTotalCommitCount = await repository.RunGitAsync("rev-list", "--count", "HEAD");
        properties["git.total.commit.count"].Should().Be(expectedTotalCommitCount);

        bool expectedDirty = await repository.IsDirtyAsync();
        properties["git.dirty"].Should().Be(expectedDirty ? "true" : "false");

        // SDK default when $(Version) isn't set.
        properties["git.build.version"].Should().Be("1.0.0");

        DateTimeOffset.TryParse(properties["git.build.time"], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset buildTime).Should().BeTrue(
            "git.build.time must be a parseable, ISO-8601-with-offset timestamp, matching the style git itself uses for git.commit.time.");

        buildTime.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromMinutes(5));

        string[] expectedKeys =
        [
            "git.branch",
            "git.commit.id",
            "git.commit.id.abbrev",
            "git.commit.id.describe",
            "git.commit.time",
            "git.commit.message.short",
            "git.commit.message.full",
            "git.commit.user.name",
            "git.commit.user.email",
            "git.build.host",
            "git.build.user.name",
            "git.build.user.email",
            "git.tags",
            "git.closest.tag.name",
            "git.closest.tag.commit.count",
            "git.remote.origin.url",
            "git.total.commit.count",
            "git.dirty",
            "git.build.version",
            "git.build.time"
        ];

        properties.Keys.Should().BeEquivalentTo(expectedKeys);
    }
}
