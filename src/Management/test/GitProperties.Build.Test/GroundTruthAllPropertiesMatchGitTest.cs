// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace Steeltoe.Management.GitProperties.Build.Test;

// Automated regression tests for the Steeltoe.Management.GitProperties.Build project
// (build/Steeltoe.Management.GitProperties.Build.targets plus its compiled MSBuild tasks). Exercises the
// scenarios that were manually verified while building this feature: ground-truth property values,
// incremental cache behavior, publish (with and without a prior build), the warn/info/skip diagnostic
// paths, shallow clones, non-ASCII commit data, and cross-project cache sharing. Every test runs against
// an isolated temporary workspace containing the CURRENT source of Steeltoe.Management.GitProperties.Build
// (see GitPropertiesTestWorkspace), not a stale git-tracked copy, so it always exercises whatever is on
// disk right now. Every git repository a test operates against is a small, synthetic one created from
// scratch (`git init` plus a handful of manufactured commits) - never a clone of this (large, real)
// repository - so the suite stays fast. Nothing here touches this repository's own working tree.
//
// One class per test (see GitPropertiesBuildTestBase's own remarks for why) rather than many [Fact]
// methods on one shared class: xUnit v3 runs different test classes concurrently by default, but never
// parallelizes methods within the same class. Every test here is dominated by "dotnet build"/"publish"
// subprocess time that's mostly I/O/wait-bound, not CPU-bound - splitting this way lets the whole suite's
// wall-clock approach its single slowest test instead of the sum of all of them.
//
// Measured (TRX per-test timing, sequential run): every test costs roughly 3.7-4.2 seconds PER "dotnet
// build"/"publish" subprocess it spawns, almost regardless of git-repository complexity - even the two
// tests with no git repository at all still cost ~3.7s each, matching the single-build cases with a real
// repository. Git setup (git init, commits, tags, config) is comparatively free by contrast. When a new
// scenario needs only one extra assertion against a plain default build, prefer folding it into an
// existing test that already builds one (see GroundTruthAllPropertiesMatchGitTest's own remarks for an
// example) over adding a dedicated test that pays for another subprocess just to re-run the identical
// setup.
public sealed class GroundTruthAllPropertiesMatchGitTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// Also folds in two other checks against this same build, rather than paying for a dedicated "dotnet build" subprocess (by far the dominant cost of any
    /// test in this suite - see the class remarks) just to exercise a single extra assertion against an otherwise identical, plain default build: that the
    /// fallback file is never written unless explicitly opted into (see WriteToProjectDirectoryCreatesFallbackFileOnBuildTest for the positive case, which -
    /// unlike this one - genuinely needs its own build, since it passes a different property), and that writing git.properties is confirmed at default
    /// verbosity.
    /// </summary>
    [Fact]
    public async Task GroundTruth_AllPropertiesMatchGit()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 3);
        string result = await repository.TestApp.BuildAsync();

        repository.TestApp.FallbackGitPropertiesGenerated.Should().BeFalse(
            "the fallback file must not be written into the project directory unless explicitly opted into.");

        string expectedRelativePath = Path.Combine("obj", "Debug", TestPaths.TestAppTargetFramework, "git.properties");
        result.Should().Contain($"git.properties: writing to '{expectedRelativePath}' for project '{GitPropertiesTestWorkspace.TestAppProjectName}'.");

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

        buildTime.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromMinutes(5), "git.build.time must reflect roughly when this build actually ran.");

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
