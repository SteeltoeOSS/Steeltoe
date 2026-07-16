// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text.RegularExpressions;

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
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 3);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult result = await ProcessRunner.RunDotnetAsync(testApp, "build");
        AssertBuildSucceeded(result, "build");

        File.Exists(GetFallbackFilePath(testApp)).Should().BeFalse(
            "the fallback file must not be written into the project directory unless explicitly opted into.");

        string expectedRelativePath = Path.Combine("obj", "Debug", TestPaths.TestAppTargetFramework, "git.properties");
        result.Output.Should().Contain($"git.properties: writing to '{expectedRelativePath}' for project '{GitPropertiesTestWorkspace.TestAppProjectName}'.");

        Dictionary<string, string> properties = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));

        string expectedCommitId = await ProcessRunner.GetGitOutputAsync(repository, "rev-parse", "HEAD");
        properties["git.commit.id"].Should().Be(expectedCommitId);

        string expectedCommitIdAbbrev = await ProcessRunner.GetGitOutputAsync(repository, "rev-parse", "--short=7", "HEAD");
        properties["git.commit.id.abbrev"].Should().Be(expectedCommitIdAbbrev);

        string expectedCommitUserName = await ProcessRunner.GetGitOutputAsync(repository, "log", "-1", "--format=%an");
        properties["git.commit.user.name"].Should().Be(expectedCommitUserName);

        string expectedCommitUserEmail = await ProcessRunner.GetGitOutputAsync(repository, "log", "-1", "--format=%ae");
        properties["git.commit.user.email"].Should().Be(expectedCommitUserEmail);

        string expectedCommitMessageShort = await ProcessRunner.GetGitOutputAsync(repository, "log", "-1", "--format=%s");
        properties["git.commit.message.short"].Should().Be(expectedCommitMessageShort);

        string expectedTotalCommitCount = await ProcessRunner.GetGitOutputAsync(repository, "rev-list", "--count", "HEAD");
        properties["git.total.commit.count"].Should().Be(expectedTotalCommitCount);

        string gitStatus = await ProcessRunner.GetGitOutputAsync(repository, "status", "--porcelain");
        bool expectedDirty = gitStatus.Length > 0;
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

public sealed class IncrementalBuildCacheSkipsButDirtyStaysLiveTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task IncrementalBuild_CacheSkipsButDirtyStaysLive()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult result1 = await ProcessRunner.RunDotnetAsync(testApp, "build");
        AssertBuildSucceeded(result1, "first build");
        string cacheFile = Path.Combine(repository, "obj", "_GitProperties", "git.properties.cache");
        File.Exists(cacheFile).Should().BeTrue("the cache file should exist after first build.");

        ProcessResult result2 = await ProcessRunner.RunDotnetAsync(testApp, "build", "-v:detailed");
        AssertBuildSucceeded(result2, "second build");

        // "Skipping target" is itself the deterministic, sufficient proof that nothing rewrote the
        // cache file on this second build - no last-write-time comparison (and the sleep it would
        // otherwise need, to guarantee a detectably different timestamp) is needed on top of it.
        result2.Output.Should().Contain("Skipping target \"GenerateGitPropertiesCache\"");

        Dictionary<string, string> properties = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));
        properties.Should().ContainKey("git.dirty");
    }
}

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

        ProcessResult result1 = await ProcessRunner.RunDotnetAsync(testApp, "build");
        AssertBuildSucceeded(result1, "first build");
        Dictionary<string, string> propertiesBefore = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));

        // git.build.time is formatted "yyyy-MM-ddTHH:mm:sszzz" (ComposeGitPropertiesTask) - second
        // resolution only, no fractional part - so two builds landing within the same wall-clock
        // second would produce identical values no matter how this test is written. This delay is
        // sized to that format's own precision, not incidental slack.
        await Task.Delay(TimeSpan.FromMilliseconds(1100), TestContext.Current.CancellationToken);

        ProcessResult result2 = await ProcessRunner.RunDotnetAsync(testApp, "build");
        AssertBuildSucceeded(result2, "second build");
        Dictionary<string, string> propertiesAfter = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));

        propertiesAfter["git.build.time"].Should().NotBe(propertiesBefore["git.build.time"],
            "git.build.time must be recomputed on every build, not reused from the shared cache.");

        propertiesAfter["git.commit.time"].Should().Be(propertiesBefore["git.commit.time"],
            "git.commit.time must stay tied to the (unchanged) commit, unlike git.build.time.");

        propertiesAfter["git.commit.id"].Should().Be(propertiesBefore["git.commit.id"], "nothing about the commit itself changed between the two builds.");
    }
}

public sealed class NewTagInvalidatesCacheTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// Also folds in coverage for the trickiest shape ParseDescribeOutput's dash-splitting has to get right - a tag name that itself contains a dash
    /// ("release-1.0"), combined with a nonzero commits-ahead count - rather than spinning up a dedicated test just for that. Tagging an ANCESTOR of HEAD,
    /// not HEAD itself, serves both purposes at once: it produces that nonzero count, and it keeps HEAD's own commit ID unchanged, which is the actual point
    /// of this test's name - proving a new tag ref alone still invalidates the shared cache (see the regression this guards against in
    /// GenerateGitPropertiesCacheTask.TryGenerateAndWriteCache's own remarks).
    /// </summary>
    [Fact]
    public async Task NewTag_InvalidatesCache()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult result1 = await ProcessRunner.RunDotnetAsync(testApp, "build");
        AssertBuildSucceeded(result1, "first build");
        Dictionary<string, string> propertiesBefore = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));
        propertiesBefore["git.tags"].Should().BeEmpty();

        string ancestorCommitId = await ProcessRunner.GetGitOutputAsync(repository, "rev-parse", "HEAD~1");
        ProcessResult tagResult = await ProcessRunner.RunGitAsync(repository, "tag", "release-1.0", ancestorCommitId);
        tagResult.ExitCode.Should().Be(0, "creating the test tag should succeed.");

        ProcessResult result2 = await ProcessRunner.RunDotnetAsync(testApp, "build");
        AssertBuildSucceeded(result2, "second build");
        Dictionary<string, string> propertiesAfter = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));

        propertiesAfter["git.tags"].Should().BeEmpty("the tag points at an ancestor, not HEAD, so it must not show up in git.tags.");
        propertiesAfter["git.closest.tag.name"].Should().Be("release-1.0");
        propertiesAfter["git.closest.tag.commit.count"].Should().Be("1", "HEAD is exactly one commit ahead of the tagged ancestor.");

        // "release-1.0-1", not the raw "git describe" output ("release-1.0-1-g<sha>"):
        // git.commit.id.describe deliberately omits the abbreviated SHA - see
        // GenerateGitPropertiesCacheTask.ParseDescribeOutput's own BaseDescribe reconstruction.
        propertiesAfter["git.commit.id.describe"].Should().Be("release-1.0-1");
    }
}

public sealed class PublishIncludesGitPropertiesTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Publish_IncludesGitProperties()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult result = await ProcessRunner.RunDotnetAsync(testApp, "publish");
        AssertBuildSucceeded(result, "publish");
        result.Output.Should().NotContain("duplicate");

        Dictionary<string, string> properties = await PropertiesFile.ReadAsync(GetReleasePublishGitPropertiesFilePath(testApp));
        string expectedCommitId = await ProcessRunner.GetGitOutputAsync(repository, "rev-parse", "HEAD");
        properties["git.commit.id"].Should().Be(expectedCommitId);
    }
}

public sealed class PublishNoBuildIncludesGitPropertiesTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Publish_NoBuild_IncludesGitProperties()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult buildResult = await ProcessRunner.RunDotnetAsync(testApp, "build", "-c", "Release");
        AssertBuildSucceeded(buildResult, "build");

        ProcessResult publishResult = await ProcessRunner.RunDotnetAsync(testApp, "publish", "-c", "Release", "--no-build");
        AssertBuildSucceeded(publishResult, "publish --no-build");
        publishResult.Output.Should().NotContain("duplicate");

        Dictionary<string, string> properties = await PropertiesFile.ReadAsync(GetReleasePublishGitPropertiesFilePath(testApp));
        string expectedCommitId = await ProcessRunner.GetGitOutputAsync(repository, "rev-parse", "HEAD");
        properties["git.commit.id"].Should().Be(expectedCommitId);
    }
}

public sealed class NoGitWarnsByDefaultTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task NoGit_WarnsByDefault()
    {
        string projectDirectory = Path.Combine(Workspace.RootDirectory, "proj");
        Directory.CreateDirectory(projectDirectory);
        string testApp = await Workspace.CopyCurrentProjectFilesAsync(projectDirectory);

        ProcessResult result = await ProcessRunner.RunDotnetAsync(testApp, "build");
        AssertBuildSucceeded(result, "the build with no .git present");
        AssertWarned(result, "GITPROPS001");
        AssertNoGitPropertiesGenerated(testApp);
    }
}

public sealed class NoGitInfoWhenEnableWarningsFalseTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task NoGit_InfoWhenEnableWarningsFalse()
    {
        string projectDirectory = Path.Combine(Workspace.RootDirectory, "proj");
        Directory.CreateDirectory(projectDirectory);
        string testApp = await Workspace.CopyCurrentProjectFilesAsync(projectDirectory);

        ProcessResult result = await ProcessRunner.RunDotnetAsync(testApp, "build", "-p:GitPropertiesEnableWarnings=false", "-v:normal");
        AssertBuildSucceeded(result, "build");
        AssertReportedAsInfoOnly(result, "GITPROPS001", "no usable .git directory found above");
        AssertNoGitPropertiesGenerated(testApp);
    }
}

public sealed class GitFileWarnsByDefaultTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task GitFile_WarnsByDefault()
    {
        string projectDirectory = Path.Combine(Workspace.RootDirectory, "proj");
        Directory.CreateDirectory(projectDirectory);
        string testApp = await Workspace.CopyCurrentProjectFilesAsync(projectDirectory);
        // ".git" must sit above BOTH TestApp and Steeltoe.Management.GitProperties.Build for the repo-root walk
        // (which starts at TestApp, the project actually being built) to find it - i.e. at
        // projectDirectory itself.
        await File.WriteAllTextAsync(Path.Combine(projectDirectory, ".git"), "gitdir: /some/where/.git/worktrees/proj", TestContext.Current.CancellationToken);

        ProcessResult defaultResult = await ProcessRunner.RunDotnetAsync(testApp, "build");
        AssertBuildSucceeded(defaultResult, "the build with .git as a file (a worktree/submodule checkout - e.g. an AI agent - must never fail)");
        AssertWarned(defaultResult, "GITPROPS002");
        AssertNoGitPropertiesGenerated(testApp);

        ProcessResult enableWarningsFalseResult = await ProcessRunner.RunDotnetAsync(testApp, "build", "-p:GitPropertiesEnableWarnings=false", "-v:normal");
        AssertBuildSucceeded(enableWarningsFalseResult, "build");
        AssertReportedAsInfoOnly(enableWarningsFalseResult, "GITPROPS002", "resolves to a git worktree or submodule");

        ProcessResult featureOffResult = await ProcessRunner.RunDotnetAsync(testApp, "build", "-p:GenerateGitProperties=false");
        AssertBuildSucceeded(featureOffResult, "build with GenerateGitProperties=false");
        featureOffResult.Output.Should().NotContain("GITPROPS002");
    }
}

public sealed class MultipleRemotesOnlyOriginUrlIsUsedTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// GenerateGitPropertiesCacheTask.ReadConfig only recognizes the literal "remote.origin.url" config key - a repository with additional remotes (a fork's
    /// "upstream", a CI mirror, etc.) must still resolve git.remote.origin.url to origin's own URL, never another remote's. Also confirms that when origin
    /// itself has more than one configured URL (via "git remote set-url --add"), the field resolves to the LAST one, matching "git config --list"'s own
    /// last-value-wins behavior for repeated keys (verified independently against a real git binary before writing this test). The winning URL is
    /// deliberately given embedded credentials, folding StripUserInfo's own coverage into this same build rather than spinning up a dedicated test just for
    /// that: proves credentials are stripped from whichever URL actually wins, not just from a hypothetical single-remote case.
    /// </summary>
    [Fact]
    public async Task MultipleRemotes_OnlyOriginUrlIsUsed()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        await ProcessRunner.RunGitAsync(repository, "remote", "add", "upstream", "https://example.com/upstream.git");
        await ProcessRunner.RunGitAsync(repository, "remote", "add", "origin", "https://example.com/origin.git");
        await ProcessRunner.RunGitAsync(repository, "remote", "set-url", "--add", "origin", "https://user:pass@example.com/origin-second.git");

        ProcessResult result = await ProcessRunner.RunDotnetAsync(testApp, "build");
        AssertBuildSucceeded(result, "build with multiple remotes configured");

        Dictionary<string, string> properties = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));

        properties["git.remote.origin.url"].Should().Be("https://example.com/origin-second.git",
            "origin's own last-configured URL must win, ignoring both the unrelated 'upstream' remote and origin's own first URL - and its embedded " +
            "'user:pass@' credentials must be stripped before the value ever reaches the cache file.");
    }
}

public sealed class ShallowCloneLeavesCommitCountsEmptyTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task ShallowClone_LeavesCommitCountsEmpty()
    {
        string source = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "source"), 3);
        await ProcessRunner.RunGitAsync(source, "tag", "v1.0.0");

        string shallow = Path.Combine(Workspace.RootDirectory, "shallow");
        // --no-local is required here: for a plain local filesystem path, git's local-clone
        // optimization bypasses shallow-transfer logic entirely and --depth is silently ignored,
        // producing a full clone that would make this test worthless.
        ProcessResult cloneResult = await ProcessRunner.RunGitAsync(Path.GetTempPath(), "clone", "--quiet", "--no-local", "--depth", "1", source, shallow);
        cloneResult.ExitCode.Should().Be(0, "shallow clone should succeed.");
        string isShallowRepository = await ProcessRunner.GetGitOutputAsync(shallow, "rev-parse", "--is-shallow-repository");
        isShallowRepository.Should().Be("true");

        string testApp = await Workspace.CopyCurrentProjectFilesAsync(shallow);

        ProcessResult result = await ProcessRunner.RunDotnetAsync(testApp, "build");
        AssertBuildSucceeded(result, "the build against a shallow clone");
        result.Output.Should().NotContain("GITPROPS001");
        result.Output.Should().NotContain("GITPROPS002");
        AssertWarned(result, "GITPROPS006");

        Dictionary<string, string> properties = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));
        properties["git.total.commit.count"].Should().BeEmpty();
        properties["git.closest.tag.commit.count"].Should().BeEmpty();
    }
}

public sealed class ShallowCloneInfoWhenEnableWarningsFalseTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// GITPROPS006 (unlike GITPROPS001-005) never blocks generation - the shallow clone is still fully usable, just with two fields left empty (see
    /// <see cref="ShallowCloneLeavesCommitCountsEmptyTest" />). Confirms $(GitPropertiesEnableWarnings) downgrades it to an informational message the same
    /// way it does for the others.
    /// </summary>
    [Fact]
    public async Task ShallowClone_InfoWhenEnableWarningsFalse()
    {
        string source = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "source"), 1);

        string shallow = Path.Combine(Workspace.RootDirectory, "shallow");
        ProcessResult cloneResult = await ProcessRunner.RunGitAsync(Path.GetTempPath(), "clone", "--quiet", "--no-local", "--depth", "1", source, shallow);
        cloneResult.ExitCode.Should().Be(0, "shallow clone should succeed.");

        string testApp = await Workspace.CopyCurrentProjectFilesAsync(shallow);

        ProcessResult result = await ProcessRunner.RunDotnetAsync(testApp, "build", "-p:GitPropertiesEnableWarnings=false", "-v:normal");
        AssertBuildSucceeded(result, "build");
        AssertReportedAsInfoOnly(result, "GITPROPS006", "repository is a shallow clone");
    }
}

public sealed class NonAsciiCommitDataRendersCorrectlyTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task NonAscii_CommitDataRendersCorrectly()
    {
        string repository = Path.Combine(Workspace.RootDirectory, "repo");
        Directory.CreateDirectory(repository);
        await ProcessRunner.RunGitAsync(repository, "init", "--quiet", "--initial-branch=main", ".");
        // \u-escaped rather than literal, so this source file itself stays plain ASCII: renders as accented Latin-1
        // supplement letters plus the trailing three characters of "commit", spelled out in Japanese (CJK).
        const string nonAsciiUserName = "\u00DCn\u00EFc\u00F6d\u00E9 T\u00EBst";
        const string nonAsciiCommitMessage = "\u00DCn\u00EFc\u00F6d\u00E9 t\u00EBst commit \u65E5\u672C\u8A9E";

        await ProcessRunner.RunGitAsync(repository, "config", "user.name", nonAsciiUserName);
        await ProcessRunner.RunGitAsync(repository, "config", "user.email", "test@example.com");
        await File.WriteAllTextAsync(Path.Combine(repository, ".gitignore"), "bin/\r\nobj/\r\n", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(repository, "file.txt"), "content", TestContext.Current.CancellationToken);
        await ProcessRunner.RunGitAsync(repository, "add", "-A");
        await ProcessRunner.RunGitAsync(repository, "commit", "--quiet", "-m", nonAsciiCommitMessage);

        string testApp = await Workspace.CopyCurrentProjectFilesAsync(repository);

        ProcessResult result = await ProcessRunner.RunDotnetAsync(testApp, "build");
        AssertBuildSucceeded(result, "build");

        Dictionary<string, string> properties = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));
        properties["git.commit.user.name"].Should().Be(nonAsciiUserName);
        properties["git.commit.message.short"].Should().Be(nonAsciiCommitMessage);
    }
}

public sealed class MultiProjectSharesCacheTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task MultiProject_SharesCache()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 2);

        // Two independent projects at the repo root, each with their own ProjectReference/Import
        // pointing at the SAME sibling Steeltoe.Management.GitProperties.Build copy (CreateSyntheticRepo
        // already placed TestApp there; reuse that exact relative layout for ProjectA/ProjectB by placing them
        // at the repo root too, siblings of TestApp and "src").
        await GitPropertiesTestWorkspace.WriteAppProjectAsync(repository, "ProjectA");
        await GitPropertiesTestWorkspace.WriteAppProjectAsync(repository, "ProjectB");

        string projectA = Path.Combine(repository, "ProjectA");
        string projectB = Path.Combine(repository, "ProjectB");

        ProcessResult resultA = await ProcessRunner.RunDotnetAsync(projectA, "build", "-v:detailed");
        AssertBuildSucceeded(resultA, "ProjectA build");

        resultA.Output.Should().Contain("git.properties: generating shared cache",
            "ProjectA (first to build) should be the one that actually generates the shared cache.");

        string cacheFile = Path.Combine(repository, "obj", "_GitProperties", "git.properties.cache");
        File.Exists(cacheFile).Should().BeTrue("ProjectA's build should have generated the shared cache.");

        ProcessResult resultB = await ProcessRunner.RunDotnetAsync(projectB, "build", "-v:detailed");
        AssertBuildSucceeded(resultB, "ProjectB build");

        // "did not log generating the cache" is itself the deterministic, sufficient proof ProjectB
        // reused ProjectA's cache instead of rewriting it - no last-write-time comparison (and the
        // sleep it would otherwise need) is needed on top of it.
        resultB.Output.Should().NotContain("git.properties: generating shared cache", "ProjectB should reuse ProjectA's cache instead of regenerating it.");

        Dictionary<string, string> propertiesA = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(projectA));
        Dictionary<string, string> propertiesB = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(projectB));
        propertiesB["git.commit.id"].Should().Be(propertiesA["git.commit.id"]);
    }
}

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

        ProcessResult result1 = await ProcessRunner.RunDotnetAsync(testApp, "build");
        AssertBuildSucceeded(result1, "multi-targeted build");

        string expectedCommitId = await ProcessRunner.GetGitOutputAsync(repository, "rev-parse", "HEAD");
        List<Dictionary<string, string>> propertiesBefore = await GetGitPropertiesPerTargetFrameworkAsync(testApp, frameworks);

        foreach (Dictionary<string, string> properties in propertiesBefore)
        {
            properties["git.commit.id"].Should().Be(expectedCommitId);
            properties["git.tags"].Should().BeEmpty();
        }

        ProcessResult tagResult = await ProcessRunner.RunGitAsync(repository, "tag", "v1.0.0");
        tagResult.ExitCode.Should().Be(0, "creating the test tag should succeed.");

        ProcessResult result2 = await ProcessRunner.RunDotnetAsync(testApp, "build");
        AssertBuildSucceeded(result2, "second multi-targeted build");

        List<Dictionary<string, string>> propertiesAfter = await GetGitPropertiesPerTargetFrameworkAsync(testApp, frameworks);

        foreach (Dictionary<string, string> properties in propertiesAfter)
        {
            properties["git.tags"].Should().Be("v1.0.0", "both target frameworks must observe the new tag, even though the commit it points at didn't change.");
        }
    }
}

public sealed class WriteToProjectDirectoryCreatesFallbackFileOnBuildTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task WriteToProjectDirectory_CreatesFallbackFile_OnBuild()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1, true);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult result1 = await ProcessRunner.RunDotnetAsync(testApp, "build", "-p:GitPropertiesWriteToProjectDirectory=true");
        AssertBuildSucceeded(result1, "build with GitPropertiesWriteToProjectDirectory=true");

        result1.Output.Should().Contain(
            $"git.properties: writing fallback copy to '{GetFallbackFilePath(testApp)}' for project '{GitPropertiesTestWorkspace.TestAppProjectName}'.");

        File.Exists(GetFallbackFilePath(testApp)).Should().BeTrue("the fallback file should have been written next to the .csproj.");

        Dictionary<string, string> fallbackProperties = await PropertiesFile.ReadAsync(GetFallbackFilePath(testApp));
        Dictionary<string, string> outputProperties1 = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));
        fallbackProperties.Should().BeEquivalentTo(outputProperties1, "the fallback file must carry the exact same content as the live build output.");

        string gitStatus = await ProcessRunner.GetGitOutputAsync(repository, "status", "--porcelain");
        gitStatus.Should().BeEmpty("the fallback file is gitignored, so it must not show up as an untracked change.");

        // A gitignored fallback file left over from the first build must not itself make a LATER build see the tree as dirty.
        ProcessResult result2 = await ProcessRunner.RunDotnetAsync(testApp, "build", "-p:GitPropertiesWriteToProjectDirectory=true");
        AssertBuildSucceeded(result2, "second build");

        Dictionary<string, string> outputProperties2 = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));

        outputProperties2["git.dirty"].Should().Be("false",
            "the gitignored fallback file from the first build must not make a later build see the tree as dirty.");
    }
}

public sealed class FallbackFileWithoutGitignoreMakesLaterBuildsAppearDirtyTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// The negative counterpart to <see cref="WriteToProjectDirectoryCreatesFallbackFileOnBuildTest" /> - proves the README's ".gitignore this file" warning
    /// is describing a real consequence, not a hypothetical one: deliberately uses a repository WITHOUT the fallback file gitignored, so the file the first
    /// build writes is left behind as a genuine untracked change - permanently flipping git.dirty to "true" on every later build, even though nothing about
    /// the actually-tracked source changed in between.
    /// </summary>
    [Fact]
    public async Task FallbackFile_WithoutGitignore_MakesLaterBuildsAppearDirty()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult result1 = await ProcessRunner.RunDotnetAsync(testApp, "build", "-p:GitPropertiesWriteToProjectDirectory=true");
        AssertBuildSucceeded(result1, "first build, which writes the (not yet gitignored) fallback file");

        string gitStatus = await ProcessRunner.GetGitOutputAsync(repository, "status", "--porcelain");
        gitStatus.Should().NotBeEmpty("the freshly-written, ungitignored fallback file should show up as an untracked change.");

        ProcessResult result2 = await ProcessRunner.RunDotnetAsync(testApp, "build", "-p:GitPropertiesWriteToProjectDirectory=true");
        AssertBuildSucceeded(result2, "second build");

        Dictionary<string, string> properties2 = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));

        properties2["git.dirty"].Should().Be("true",
            "the ungitignored fallback file left over from the first build makes every later build see the tree as dirty.");
    }
}

public sealed class WriteToProjectDirectoryCreatesFallbackFileOnPublishTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// "dotnet publish" runs its own compile/composition steps internally regardless of whether "dotnet build" ran first - this guards against the fallback
    /// file only being written along the "build" target chain and silently never firing when publish is the very first command run against a fresh checkout
    /// (a common real-world pattern: `dotnet publish` directly, without a separate build step). Runs with $(GitPropertiesEnableWarnings) at its default
    /// (enabled) setting to confirm nothing about the fallback-writing path implicitly depends on warnings being suppressed - since a real .git repository
    /// is available here, nothing should be skipped (and no GITPROPS0xx code should appear) regardless of that setting.
    /// </summary>
    [Fact]
    public async Task WriteToProjectDirectory_CreatesFallbackFile_OnPublish()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1, true);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult result = await ProcessRunner.RunDotnetAsync(testApp, "publish", "-p:GitPropertiesWriteToProjectDirectory=true",
            "-p:GitPropertiesEnableWarnings=true");

        AssertBuildSucceeded(result, "publish with GitPropertiesWriteToProjectDirectory=true, without an upfront build");

        result.Output.Should().NotContain("GITPROPS0",
            "nothing should be skipped, and no fallback should be needed, when a real .git repository is available.");

        File.Exists(GetFallbackFilePath(testApp)).Should().BeTrue("the fallback file should have been written next to the .csproj, even for a bare publish.");

        Dictionary<string, string> fallbackProperties = await PropertiesFile.ReadAsync(GetFallbackFilePath(testApp));
        Dictionary<string, string> publishedProperties = await PropertiesFile.ReadAsync(GetReleasePublishGitPropertiesFilePath(testApp));
        fallbackProperties.Should().BeEquivalentTo(publishedProperties, "the fallback file must carry the exact same content as the published output.");

        string gitStatus = await ProcessRunner.GetGitOutputAsync(repository, "status", "--porcelain");
        gitStatus.Should().BeEmpty("the fallback file is gitignored, so it must not show up as an untracked change.");
    }
}

public sealed class FallbackFileIgnoredWhenLiveGitAvailableTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// Guards against a stale fallback file (left over from some earlier build) ever shadowing live generation - the fallback file must only ever be used as
    /// a last resort, never preferred over a real, currently-usable .git repository.
    /// </summary>
    [Fact]
    public async Task FallbackFile_Ignored_WhenLiveGitAvailable()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1, true);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        await File.WriteAllLinesAsync(GetFallbackFilePath(testApp), ["git.commit.id=deadbeefdeadbeefdeadbeefdeadbeefdeadbeef"],
            TestContext.Current.CancellationToken);

        ProcessResult result = await ProcessRunner.RunDotnetAsync(testApp, "build", "-v:detailed");
        AssertBuildSucceeded(result, "build with a stale fallback file present alongside a real .git repository");
        result.Output.Should().NotContain("using pre-generated fallback file", "the fallback notice must not appear when live generation actually ran.");

        Dictionary<string, string> properties = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));
        string expectedCommitId = await ProcessRunner.GetGitOutputAsync(repository, "rev-parse", "HEAD");
        properties["git.commit.id"].Should().Be(expectedCommitId);
    }
}

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

public sealed class WriteGitPropertiesFallbackFileProducesFallbackFileWithoutCompilingTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// The stable, documented entry point for step 1 of the "Recommended cf push workflow" (see PackageReadme.md) - confirms it actually produces a usable
    /// fallback file, and that doing so never compiles anything (the whole reason to prefer it over a full "dotnet build" before a source push).
    /// </summary>
    [Fact]
    public async Task WriteGitPropertiesFallbackFile_ProducesFallbackFile_WithoutCompiling()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1, true);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult result = await ProcessRunner.RunDotnetAsync(testApp, "build", "-t:WriteGitPropertiesFallbackFile");
        AssertBuildSucceeded(result, "build -t:WriteGitPropertiesFallbackFile");

        File.Exists(GetFallbackFilePath(testApp)).Should().BeTrue("the fallback file should have been written next to the .csproj.");
        Dictionary<string, string> properties = await PropertiesFile.ReadAsync(GetFallbackFilePath(testApp));
        string expectedCommitId = await ProcessRunner.GetGitOutputAsync(repository, "rev-parse", "HEAD");
        properties["git.commit.id"].Should().Be(expectedCommitId);

        // "bin\Debug\<TFM>\publish" gets created as empty, routine SDK scaffolding even here (PrepareForPublish's own setup) - checking for the absence
        // of the compiled assembly itself, not just the bin directory, is what actually proves no compilation happened.
        File.Exists(Path.Combine(testApp, "bin", "Debug", TestPaths.TestAppTargetFramework, $"{GitPropertiesTestWorkspace.TestAppProjectName}.dll")).Should()
            .BeFalse("this target must never compile the project - that's the whole point of using it instead of a full build before a source push.");
    }
}

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

        ProcessResult writeResult = await ProcessRunner.RunDotnetAsync(testApp, "build", "-t:WriteGitPropertiesFallbackFile");
        AssertBuildSucceeded(writeResult, "build -t:WriteGitPropertiesFallbackFile");
        Dictionary<string, string> fallbackProperties = await PropertiesFile.ReadAsync(GetFallbackFilePath(testApp));

        string pushedRoot = GitPropertiesTestWorkspace.SimulateSourcePush(repository, Path.Combine(Workspace.RootDirectory, "pushed"));
        string pushedApp = Path.Combine(pushedRoot, GitPropertiesTestWorkspace.TestAppProjectName);
        File.Exists(GetFallbackFilePath(pushedApp)).Should().BeTrue("the fallback git.properties must have survived the simulated push.");

        ProcessResult publishResult = await ProcessRunner.RunDotnetAsync(pushedApp, "publish", "-v:detailed");
        AssertBuildSucceeded(publishResult, "publish with no usable .git repository present");

        publishResult.Output.Should().Contain(
            "using pre-generated fallback file", "using the fallback should still be traceable, so it's never silently stale.");

        Dictionary<string, string> publishedProperties = await PropertiesFile.ReadAsync(GetReleasePublishGitPropertiesFilePath(pushedApp));
        publishedProperties.Should().BeEquivalentTo(fallbackProperties, "the fallback-produced output must exactly match the pre-generated fallback content.");
    }
}

public sealed class WriteGitPropertiesFallbackFileWorksWithNoRestoreTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// "--no-restore" must work the same way for this target as for any other build invocation - it only requires that restore already happened at least
    /// once, same as a normal build.
    /// </summary>
    [Fact]
    public async Task WriteGitPropertiesFallbackFile_WorksWithNoRestore()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1, true);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult restoreResult = await ProcessRunner.RunDotnetAsync(testApp, "restore");
        AssertBuildSucceeded(restoreResult, "restore");

        ProcessResult result = await ProcessRunner.RunDotnetAsync(testApp, "build", "--no-restore", "-t:WriteGitPropertiesFallbackFile");
        AssertBuildSucceeded(result, "build --no-restore -t:WriteGitPropertiesFallbackFile");

        File.Exists(GetFallbackFilePath(testApp)).Should().BeTrue();
    }
}

public sealed class WriteGitPropertiesFallbackFileThenPublishNoBuildFailsTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// Documents/guards the one real caveat called out in PackageReadme.md: this target never produces real build output, so a local "dotnet publish
    /// --no-build" afterward must fail - there is nothing compiled to publish. If this target's own implementation ever accidentally started producing
    /// compiled output (defeating its "lightweight" purpose), this test would start failing for the opposite reason (publish --no-build would start
    /// succeeding) - a signal to revisit the target, not just delete this test.
    /// </summary>
    [Fact]
    public async Task WriteGitPropertiesFallbackFile_ThenPublishNoBuild_Fails()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1, true);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult writeResult = await ProcessRunner.RunDotnetAsync(testApp, "build", "-t:WriteGitPropertiesFallbackFile");
        AssertBuildSucceeded(writeResult, "build -t:WriteGitPropertiesFallbackFile");

        ProcessResult publishResult = await ProcessRunner.RunDotnetAsync(testApp, "publish", "--no-build");

        publishResult.ExitCode.Should().NotBe(0,
            "publishing --no-build after only writing the fallback file (no real build ever ran) must fail - there is no compiled output to publish.");
    }
}

public sealed class NuGetPackageConsumedViaPackageReferenceGeneratesGitPropertiesTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// Every other test here consumes Steeltoe.Management.GitProperties.Build straight from source (ProjectReference + Import) - this is the only one that
    /// goes through a real, packed .nupkg via &lt;PackageReference&gt;, the way an actual external user of the package would. That exercises the NuGet
    /// "build\{PackageId}.targets" auto-import convention end-to-end (no explicit &lt;Import&gt; anywhere in the consumer project) and the in-process
    /// (non-dev-loop) task-loading branch (SourceCheckout.txt is never packed, so it's absent in this layout - see $(GitPropertiesTaskHost) in
    /// Steeltoe.Management.GitProperties.Build.targets). Isolated per andrewlock.net's "Creating a source generator, part 3" approach: a local folder feed
    /// (just our own freshly-packed .nupkg, via a nuget.config with &lt;clear/&gt;) and a per-test RestorePackagesPath, so this never touches - or gets a
    /// stale result from - the machine-wide global-packages cache at %userprofile%\.nuget\packages.
    /// </summary>
    [Fact]
    public async Task NuGetPackage_ConsumedViaPackageReference_GeneratesGitProperties()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1);

        string feedDirectory = await Workspace.PackGitPropertiesBuildToFeedAsync();
        string packageId = await TestPaths.GetPackageIdAsync();

        string[] nuPkgFiles = Directory.GetFiles(feedDirectory, $"{packageId}.*.nupkg");
        nuPkgFiles.Should().ContainSingle("packing should produce exactly one .nupkg.");

        var nuPkgVersionRegex = new Regex($@"^{Regex.Escape(packageId)}\.(.+)\.nupkg$", RegexOptions.None, TimeSpan.FromSeconds(1));
        Match versionMatch = nuPkgVersionRegex.Match(Path.GetFileName(nuPkgFiles[0]));
        versionMatch.Success.Should().BeTrue("the .nupkg file name should embed the package version.");
        string packageVersion = versionMatch.Groups[1].Value;

        string consumerDirectory = Path.Combine(repository, "Consumer");
        await GitPropertiesTestWorkspace.CreatePackageConsumerProjectAsync(consumerDirectory, packageVersion);
        await GitPropertiesTestWorkspace.WriteIsolatedNuGetConfigAsync(Path.Combine(consumerDirectory, "nuget.config"), feedDirectory);

        string isolatedPackagesPath = Path.Combine(Workspace.RootDirectory, "isolated-packages");
        ProcessResult result = await ProcessRunner.RunDotnetAsync(consumerDirectory, "build", $"-p:RestorePackagesPath={isolatedPackagesPath}");
        AssertBuildSucceeded(result, "the build of a project consuming Steeltoe.Management.GitProperties.Build via PackageReference");

        result.Output.Should().Contain("0 Warning(s)",
            "a real package consumer should see no in-process task-loading fallback warning or any other diagnostic.");

        // NuGet always lowercases the package ID for the on-disk global-packages-folder layout - this isn't
        // an arbitrary case normalization, so ToUpperInvariant() (as generally preferred) would look here for
        // a folder that NuGet never creates.
#pragma warning disable S4040
        string lowerCasePackageId = packageId.ToLowerInvariant();
#pragma warning restore S4040

        Directory.Exists(Path.Combine(isolatedPackagesPath, lowerCasePackageId, packageVersion)).Should().BeTrue(
            "the package should restore into the isolated path, never the machine-wide global-packages cache.");

        Dictionary<string, string> properties = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(consumerDirectory));
        string expectedCommitId = await ProcessRunner.GetGitOutputAsync(repository, "rev-parse", "HEAD");
        properties["git.commit.id"].Should().Be(expectedCommitId);
    }
}

public sealed class SmartDefaultSkipsGenerationWhenNoConsumingPackageReferenceTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// The default case for the overwhelming majority of projects in a large solution (a class library, a test project, anything without a consuming package
    /// anywhere in its resolved dependency graph): generation is skipped entirely, without needing an explicit opt-out, and without breaking the build. A
    /// real git repository is deliberately present here (unlike NoGitWarnsByDefaultTest) to prove the smart default - not "no .git found" - is what causes
    /// the skip.
    /// </summary>
    [Fact]
    public async Task SmartDefault_SkipsGeneration_WhenNoConsumingPackageReference()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1);

        string testApp =
            await GitPropertiesTestWorkspace.WriteAppProjectAsync(repository, GitPropertiesTestWorkspace.TestAppProjectName, generateGitProperties: null);

        ProcessResult result = await ProcessRunner.RunDotnetAsync(testApp, "build", "-v:detailed");
        AssertBuildSucceeded(result, "build with no consuming-package reference and $(GenerateGitProperties) left at its smart default");
        // Not a numbered GITPROPS0xx code - this is plain internal trace output, not a diagnosable outcome (see the .targets file's own comment on it).
        result.Output.Should().Contain("git.properties generation skipped: no reference to");
        AssertNoGitPropertiesGenerated(testApp);
    }
}

public sealed class SmartDefaultGeneratesGitPropertiesWhenConsumingPackageReferencedTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// The positive counterpart to <see cref="SmartDefaultSkipsGenerationWhenNoConsumingPackageReferenceTest" />: a project referencing the real default
    /// consuming package ID (Steeltoe.Management.Endpoint) gets git.properties generated with no explicit $(GenerateGitProperties) needed. Uses a minimal
    /// stand-in project with that exact name/PackageId (see WriteDummyDependencyProjectAsync's remarks) rather than the real, large Endpoint project, so
    /// this test stays fast and fully offline.
    /// </summary>
    [Fact]
    public async Task SmartDefault_GeneratesGitProperties_WhenConsumingPackageReferenced()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1);
        const string consumingPackageStandInName = "Steeltoe.Management.Endpoint";
        await GitPropertiesTestWorkspace.WriteDummyDependencyProjectAsync(repository, consumingPackageStandInName);

        string testApp = await GitPropertiesTestWorkspace.WriteAppProjectAsync(repository, GitPropertiesTestWorkspace.TestAppProjectName,
            generateGitProperties: null,
            extraItemGroupContent: $"""<ProjectReference Include="..\{consumingPackageStandInName}\{consumingPackageStandInName}.csproj" />""");

        ProcessResult result = await ProcessRunner.RunDotnetAsync(testApp, "build");
        AssertBuildSucceeded(result, "build with a Steeltoe.Management.Endpoint reference and $(GenerateGitProperties) left at its smart default");

        Dictionary<string, string> properties = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));
        string expectedCommitId = await ProcessRunner.GetGitOutputAsync(repository, "rev-parse", "HEAD");
        properties["git.commit.id"].Should().Be(expectedCommitId);
    }
}

public sealed class SmartDefaultOverrideDetectsCustomPackageIdsTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// Proves $(GitPropertiesConsumingPackageIds) is genuinely overridable - for consumers of this package who don't use Steeltoe.Management.Endpoint at all
    /// (e.g. a hand-rolled /info endpoint reading git.properties directly), so the smart default isn't hardcoded away from them.
    /// </summary>
    [Fact]
    public async Task SmartDefault_Override_DetectsCustomPackageIds()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1);
        const string customPackageId = "Contoso.Actuators";
        await GitPropertiesTestWorkspace.WriteDummyDependencyProjectAsync(repository, customPackageId);

        string testApp = await GitPropertiesTestWorkspace.WriteAppProjectAsync(repository, GitPropertiesTestWorkspace.TestAppProjectName,
            generateGitProperties: null, extraItemGroupContent: $"""<ProjectReference Include="..\{customPackageId}\{customPackageId}.csproj" />""");

        ProcessResult result = await ProcessRunner.RunDotnetAsync(testApp, "build", $"-p:GitPropertiesConsumingPackageIds={customPackageId}");
        AssertBuildSucceeded(result, "build with a custom $(GitPropertiesConsumingPackageIds) matching a referenced project");

        Dictionary<string, string> properties = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));
        string expectedCommitId = await ProcessRunner.GetGitOutputAsync(repository, "rev-parse", "HEAD");
        properties["git.commit.id"].Should().Be(expectedCommitId);
    }
}

public sealed class SmartDefaultOverrideDoesNotMatchPackageIdAsPrefixTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// Guards against a regression to a naive substring match (e.g. "IndexOf(id + "/")" without also requiring the match to be a whole library key) - a
    /// project referencing only "Some2" (never "Some" itself) must NOT be detected when $(GitPropertiesConsumingPackageIds) is configured as "Some", even
    /// though "Some2" starts with "Some". Proves DetectConsumingPackageReferenceTask compares whole package IDs, not prefixes.
    /// </summary>
    [Fact]
    public async Task SmartDefault_Override_DoesNotMatchPackageIdAsPrefix()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1);
        const string longerPackageId = "Some2";
        await GitPropertiesTestWorkspace.WriteDummyDependencyProjectAsync(repository, longerPackageId);

        string testApp = await GitPropertiesTestWorkspace.WriteAppProjectAsync(repository, GitPropertiesTestWorkspace.TestAppProjectName,
            generateGitProperties: null, extraItemGroupContent: $"""<ProjectReference Include="..\{longerPackageId}\{longerPackageId}.csproj" />""");

        ProcessResult result = await ProcessRunner.RunDotnetAsync(testApp, "build", "-p:GitPropertiesConsumingPackageIds=Some");
        AssertBuildSucceeded(result, "build with a referenced package ('Some2') that is a superstring, not a match, of the configured ID ('Some')");
        AssertNoGitPropertiesGenerated(testApp);
    }
}

public sealed class SmartDefaultOverrideEmptyPackageIdsViaGlobalPropertySkipsGenerationGracefullyTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// Guards against a regression where MSBuild's required-parameter check for a Task string parameter treats an empty string the same as "not supplied":
    /// setting $(GitPropertiesConsumingPackageIds) to blank via a global property (e.g. "-p:GitPropertiesConsumingPackageIds=") reaches
    /// DetectConsumingPackageReferenceTask.PackageIds unchanged (global properties can't be reassigned by the project's own conditional default at
    /// ResolveGitPropertiesPaths above), so PackageIds must NOT be [Required] - it must instead behave exactly like "no configured ID happens to match",
    /// i.e. skip generation gracefully rather than fail the build with MSB4044.
    /// </summary>
    [Fact]
    public async Task SmartDefault_Override_EmptyPackageIdsViaGlobalProperty_SkipsGenerationGracefully()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1);
        const string consumingPackageStandInName = "Steeltoe.Management.Endpoint";
        await GitPropertiesTestWorkspace.WriteDummyDependencyProjectAsync(repository, consumingPackageStandInName);

        string testApp = await GitPropertiesTestWorkspace.WriteAppProjectAsync(repository, GitPropertiesTestWorkspace.TestAppProjectName,
            generateGitProperties: null,
            extraItemGroupContent: $"""<ProjectReference Include="..\{consumingPackageStandInName}\{consumingPackageStandInName}.csproj" />""");

        ProcessResult result = await ProcessRunner.RunDotnetAsync(testApp, "build", "-p:GitPropertiesConsumingPackageIds=");
        AssertBuildSucceeded(result, "build with $(GitPropertiesConsumingPackageIds) explicitly cleared via a global property");
        AssertNoGitPropertiesGenerated(testApp);
    }
}

public sealed class SmartDefaultExplicitFalseWinsOverDetectedConsumingPackageReferenceTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// A consumer's explicit choice must never be second-guessed by the smart default, in either direction - the negative direction (no reference, but
    /// explicitly forced on) is already exercised by every other test in this file, which all set $(GenerateGitProperties)=true explicitly via
    /// WriteAppProject's default. This covers the other direction: a consuming-package reference IS present (the smart default would say "generate"), but
    /// the consumer explicitly opted out anyway.
    /// </summary>
    [Fact]
    public async Task SmartDefault_ExplicitFalse_WinsOverDetectedConsumingPackageReference()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1);
        const string consumingPackageStandInName = "Steeltoe.Management.Endpoint";
        await GitPropertiesTestWorkspace.WriteDummyDependencyProjectAsync(repository, consumingPackageStandInName);

        string testApp = await GitPropertiesTestWorkspace.WriteAppProjectAsync(repository, GitPropertiesTestWorkspace.TestAppProjectName,
            generateGitProperties: null,
            extraItemGroupContent: $"""<ProjectReference Include="..\{consumingPackageStandInName}\{consumingPackageStandInName}.csproj" />""");

        ProcessResult result = await ProcessRunner.RunDotnetAsync(testApp, "build", "-p:GenerateGitProperties=false");
        AssertBuildSucceeded(result, "build with GenerateGitProperties explicitly set to false despite a consuming-package reference being present");
        AssertNoGitPropertiesGenerated(testApp);
    }
}
