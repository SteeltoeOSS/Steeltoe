// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text.RegularExpressions;

#pragma warning disable S2925 // "Thread.Sleep" should not be used in tests

namespace Steeltoe.Management.GitProperties.Build.Test;

/// <summary>
/// Automated regression tests for the Steeltoe.Management.GitProperties.Build project (build/Steeltoe.Management.GitProperties.Build.targets plus its
/// compiled MSBuild tasks). Exercises the scenarios that were manually verified while building this feature: ground-truth property values, incremental
/// cache behavior, publish (with and without a prior build), the warn/info/skip diagnostic paths, shallow clones, non-ASCII commit data, and
/// cross-project cache sharing. Every test runs against an isolated temporary workspace containing the CURRENT source of
/// Steeltoe.Management.GitProperties.Build (see GitPropertiesTestWorkspace), not a stale git-tracked copy, so it always exercises whatever is on disk
/// right now. Every git repository a test operates against is a small, synthetic one created from scratch (`git init` plus a handful of manufactured
/// commits) - never a clone of this (large, real) repository - so the suite stays fast. Nothing here touches this repository's own working tree.
/// </summary>
public sealed class GitPropertiesBuildTests : IDisposable
{
    private static readonly Regex NuPkgVersionRegex =
        new($@"^{Regex.Escape(TestPaths.PackageId)}\.(.+)\.nupkg$", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    private readonly GitPropertiesTestWorkspace _workspace = new();

    [Fact]
    public void GroundTruth_AllPropertiesMatchGit()
    {
        string repository = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "repo"), 3);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult result = ProcessRunner.RunDotnet(testApp, "build");
        AssertBuildSucceeded(result, "build");

        Dictionary<string, string> properties = PropertiesFile.Read(DebugGitPropertiesFile(testApp));

        properties["git.commit.id"].Should().Be(ProcessRunner.GetGitOutput(repository, "rev-parse", "HEAD"));
        properties["git.commit.id.abbrev"].Should().Be(ProcessRunner.GetGitOutput(repository, "rev-parse", "--short=7", "HEAD"));
        properties["git.commit.user.name"].Should().Be(ProcessRunner.GetGitOutput(repository, "log", "-1", "--format=%an"));
        properties["git.commit.user.email"].Should().Be(ProcessRunner.GetGitOutput(repository, "log", "-1", "--format=%ae"));
        properties["git.commit.message.short"].Should().Be(ProcessRunner.GetGitOutput(repository, "log", "-1", "--format=%s"));
        properties["git.total.commit.count"].Should().Be(ProcessRunner.GetGitOutput(repository, "rev-list", "--count", "HEAD"));

        bool expectedDirty = ProcessRunner.GetGitOutput(repository, "status", "--porcelain").Length > 0;
        properties["git.dirty"].Should().Be(expectedDirty ? "true" : "false");

        // SDK default when $(Version) isn't set.
        properties["git.build.version"].Should().Be("1.0.0");

        DateTimeOffset.TryParse(properties["git.build.time"], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset buildTime).Should()
            .BeTrue("git.build.time must be a parseable, ISO-8601-with-offset timestamp, matching the style git itself uses for git.commit.time.");

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

    [Fact]
    public void IncrementalBuild_CacheSkipsButDirtyStaysLive()
    {
        string repository = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "repo"), 1);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult result1 = ProcessRunner.RunDotnet(testApp, "build");
        AssertBuildSucceeded(result1, "first build");
        string cacheFile = Path.Combine(repository, "obj", "_GitProperties", "git.properties.cache");
        File.Exists(cacheFile).Should().BeTrue("the cache file should exist after first build.");
        DateTime writeTimeBefore = File.GetLastWriteTimeUtc(cacheFile);

        // Ensure a rewritten file would get a detectably different modified-time.
        Thread.Sleep(1100);
        ProcessResult result2 = ProcessRunner.RunDotnet(testApp, "build", "-v:detailed");
        AssertBuildSucceeded(result2, "second build");
        result2.Output.Should().Contain("Skipping target \"GenerateGitPropertiesCache\"");

        File.GetLastWriteTimeUtc(cacheFile).Should().Be(writeTimeBefore, "nothing git-relevant changed, so the cache file must not be rewritten.");

        PropertiesFile.Read(DebugGitPropertiesFile(testApp)).Should().ContainKey("git.dirty");
    }

    /// <summary>
    /// Guards against git.build.time accidentally ending up in the shared, cross-project/cross-TFM cache (see GenerateGitPropertiesCacheTask) instead of
    /// being recomputed by ComposeGitPropertiesTask on every build - same class of regression IncrementalBuild_CacheSkipsButDirtyStaysLive guards against
    /// for git.dirty. A cached build time would go stale (reporting the FIRST build's time on every subsequent one), silently defeating the whole point of
    /// the field: telling you when THIS build actually ran.
    /// </summary>
    [Fact]
    public void BuildTime_ChangesAcrossBuilds_UnlikeCommitTime()
    {
        string repository = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "repo"), 1);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult result1 = ProcessRunner.RunDotnet(testApp, "build");
        AssertBuildSucceeded(result1, "first build");
        Dictionary<string, string> propertiesBefore = PropertiesFile.Read(DebugGitPropertiesFile(testApp));

        // Ensure a live-recomputed build time would get a detectably different value.
        Thread.Sleep(1100);
        ProcessResult result2 = ProcessRunner.RunDotnet(testApp, "build");
        AssertBuildSucceeded(result2, "second build");
        Dictionary<string, string> propertiesAfter = PropertiesFile.Read(DebugGitPropertiesFile(testApp));

        propertiesAfter["git.build.time"].Should().NotBe(propertiesBefore["git.build.time"],
            "git.build.time must be recomputed on every build, not reused from the shared cache.");

        propertiesAfter["git.commit.time"].Should().Be(propertiesBefore["git.commit.time"],
            "git.commit.time must stay tied to the (unchanged) commit, unlike git.build.time.");

        propertiesAfter["git.commit.id"].Should().Be(propertiesBefore["git.commit.id"], "nothing about the commit itself changed between the two builds.");
    }

    /// <summary>
    /// Also folds in coverage for the trickiest shape ParseDescribeOutput's dash-splitting has to get right - a tag name that itself contains a dash
    /// ("release-1.0"), combined with a nonzero commits-ahead count - rather than spinning up a dedicated test just for that. Tagging an ANCESTOR of HEAD,
    /// not HEAD itself, serves both purposes at once: it produces that nonzero count, and it keeps HEAD's own commit ID unchanged, which is the actual point
    /// of this test's name - proving a new tag ref alone still invalidates the shared cache (see the regression this guards against in
    /// GenerateGitPropertiesCacheTask.TryGenerateAndWriteCache's own remarks).
    /// </summary>
    [Fact]
    public void NewTag_InvalidatesCache()
    {
        string repository = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "repo"), 1);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult result1 = ProcessRunner.RunDotnet(testApp, "build");
        AssertBuildSucceeded(result1, "first build");
        Dictionary<string, string> propertiesBefore = PropertiesFile.Read(DebugGitPropertiesFile(testApp));
        propertiesBefore["git.tags"].Should().BeEmpty();

        string ancestorCommitId = ProcessRunner.GetGitOutput(repository, "rev-parse", "HEAD~1");
        ProcessResult tagResult = ProcessRunner.RunGit(repository, "tag", "release-1.0", ancestorCommitId);
        tagResult.ExitCode.Should().Be(0, "creating the test tag should succeed.");

        ProcessResult result2 = ProcessRunner.RunDotnet(testApp, "build");
        AssertBuildSucceeded(result2, "second build");
        Dictionary<string, string> propertiesAfter = PropertiesFile.Read(DebugGitPropertiesFile(testApp));

        propertiesAfter["git.tags"].Should().BeEmpty("the tag points at an ancestor, not HEAD, so it must not show up in git.tags.");
        propertiesAfter["git.closest.tag.name"].Should().Be("release-1.0");
        propertiesAfter["git.closest.tag.commit.count"].Should().Be("1", "HEAD is exactly one commit ahead of the tagged ancestor.");

        // "release-1.0-1", not the raw "git describe" output ("release-1.0-1-g<sha>"):
        // git.commit.id.describe deliberately omits the abbreviated SHA - see
        // GenerateGitPropertiesCacheTask.ParseDescribeOutput's own BaseDescribe reconstruction.
        propertiesAfter["git.commit.id.describe"].Should().Be("release-1.0-1");
    }

    /// <summary>
    /// Unlike every GITPROPS0xx diagnostic (all either $(GitPropertiesEnableWarnings)-gated or left at Message's own default importance because they
    /// describe an anomaly, not the happy path), confirmation that git.properties was actually written is unconditional and at High importance - visible in
    /// default build output with no extra verbosity flag needed - because it's the one concrete artifact this whole feature exists to produce. Logged from
    /// Steeltoe.Management.GitProperties.Build.targets (via $(MSBuildProjectName)), not from ComposeGitPropertiesTask itself - a Task has no built-in notion
    /// of "which project is this", so a solution build with many projects would otherwise be unable to tell which one this line belongs to.
    /// </summary>
    [Fact]
    public void ComposeGitProperties_LogsWrittenFileAtDefaultVerbosity()
    {
        string repository = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "repo"), 1);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult result = ProcessRunner.RunDotnet(testApp, "build");
        AssertBuildSucceeded(result, "build");

        string expectedRelativePath = Path.Combine("obj", "Debug", TestPaths.TestAppTargetFramework, "git.properties");
        result.Output.Should().Contain($"git.properties: writing to '{expectedRelativePath}' for project '{GitPropertiesTestWorkspace.TestAppProjectName}'.");
    }

    [Fact]
    public void Publish_IncludesGitProperties()
    {
        string repository = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "repo"), 1);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult result = ProcessRunner.RunDotnet(testApp, "publish");
        AssertBuildSucceeded(result, "publish");
        result.Output.Should().NotContain("duplicate");

        Dictionary<string, string> properties = PropertiesFile.Read(ReleasePublishGitPropertiesFile(testApp));
        properties["git.commit.id"].Should().Be(ProcessRunner.GetGitOutput(repository, "rev-parse", "HEAD"));
    }

    [Fact]
    public void Publish_NoBuild_IncludesGitProperties()
    {
        string repository = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "repo"), 1);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult buildResult = ProcessRunner.RunDotnet(testApp, "build", "-c", "Release");
        AssertBuildSucceeded(buildResult, "build");

        ProcessResult publishResult = ProcessRunner.RunDotnet(testApp, "publish", "-c", "Release", "--no-build");
        AssertBuildSucceeded(publishResult, "publish --no-build");
        publishResult.Output.Should().NotContain("duplicate");

        Dictionary<string, string> properties = PropertiesFile.Read(ReleasePublishGitPropertiesFile(testApp));
        properties["git.commit.id"].Should().Be(ProcessRunner.GetGitOutput(repository, "rev-parse", "HEAD"));
    }

    [Fact]
    public void NoGit_WarnsByDefault()
    {
        string projectDirectory = Path.Combine(_workspace.RootDirectory, "proj");
        Directory.CreateDirectory(projectDirectory);
        string testApp = _workspace.CopyCurrentProjectFiles(projectDirectory);

        ProcessResult result = ProcessRunner.RunDotnet(testApp, "build");
        AssertBuildSucceeded(result, "the build with no .git present");
        AssertWarned(result, "GITPROPS001");
        AssertNoGitPropertiesGenerated(testApp);
    }

    [Fact]
    public void NoGit_InfoWhenEnableWarningsFalse()
    {
        string projectDirectory = Path.Combine(_workspace.RootDirectory, "proj");
        Directory.CreateDirectory(projectDirectory);
        string testApp = _workspace.CopyCurrentProjectFiles(projectDirectory);

        ProcessResult result = ProcessRunner.RunDotnet(testApp, "build", "-p:GitPropertiesEnableWarnings=false", "-v:normal");
        AssertBuildSucceeded(result, "build");
        AssertReportedAsInfoOnly(result, "GITPROPS001", "no usable .git directory found above");
        AssertNoGitPropertiesGenerated(testApp);
    }

    [Fact]
    public void GitFile_WarnsByDefault()
    {
        string projectDirectory = Path.Combine(_workspace.RootDirectory, "proj");
        Directory.CreateDirectory(projectDirectory);
        string testApp = _workspace.CopyCurrentProjectFiles(projectDirectory);
        // ".git" must sit above BOTH TestApp and Steeltoe.Management.GitProperties.Build for the repo-root walk
        // (which starts at TestApp, the project actually being built) to find it - i.e. at
        // projectDirectory itself.
        File.WriteAllText(Path.Combine(projectDirectory, ".git"), "gitdir: /some/where/.git/worktrees/proj");

        ProcessResult defaultResult = ProcessRunner.RunDotnet(testApp, "build");
        AssertBuildSucceeded(defaultResult, "the build with .git as a file (a worktree/submodule checkout - e.g. an AI agent - must never fail)");
        AssertWarned(defaultResult, "GITPROPS002");
        AssertNoGitPropertiesGenerated(testApp);

        ProcessResult enableWarningsFalseResult = ProcessRunner.RunDotnet(testApp, "build", "-p:GitPropertiesEnableWarnings=false", "-v:normal");
        AssertBuildSucceeded(enableWarningsFalseResult, "build");
        AssertReportedAsInfoOnly(enableWarningsFalseResult, "GITPROPS002", "resolves to a git worktree or submodule");

        ProcessResult featureOffResult = ProcessRunner.RunDotnet(testApp, "build", "-p:GenerateGitProperties=false");
        AssertBuildSucceeded(featureOffResult, "build with GenerateGitProperties=false");
        featureOffResult.Output.Should().NotContain("GITPROPS002");
    }

    /// <summary>
    /// GenerateGitPropertiesCacheTask.ReadConfig only recognizes the literal "remote.origin.url" config key - a repository with additional remotes (a fork's
    /// "upstream", a CI mirror, etc.) must still resolve git.remote.origin.url to origin's own URL, never another remote's. Also confirms that when origin
    /// itself has more than one configured URL (via "git remote set-url --add"), the field resolves to the LAST one, matching "git config --list"'s own
    /// last-value-wins behavior for repeated keys (verified independently against a real git binary before writing this test). The winning URL is
    /// deliberately given embedded credentials, folding StripUserInfo's own coverage into this same build rather than spinning up a dedicated test just for
    /// that: proves credentials are stripped from whichever URL actually wins, not just from a hypothetical single-remote case.
    /// </summary>
    [Fact]
    public void MultipleRemotes_OnlyOriginUrlIsUsed()
    {
        string repository = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "repo"), 1);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessRunner.RunGit(repository, "remote", "add", "upstream", "https://example.com/upstream.git");
        ProcessRunner.RunGit(repository, "remote", "add", "origin", "https://example.com/origin.git");
        ProcessRunner.RunGit(repository, "remote", "set-url", "--add", "origin", "https://user:pass@example.com/origin-second.git");

        ProcessResult result = ProcessRunner.RunDotnet(testApp, "build");
        AssertBuildSucceeded(result, "build with multiple remotes configured");

        Dictionary<string, string> properties = PropertiesFile.Read(DebugGitPropertiesFile(testApp));

        properties["git.remote.origin.url"].Should().Be("https://example.com/origin-second.git",
            "origin's own last-configured URL must win, ignoring both the unrelated 'upstream' remote and origin's own first URL - and its embedded " +
            "'user:pass@' credentials must be stripped before the value ever reaches the cache file.");
    }

    [Fact]
    public void ShallowClone_LeavesCommitCountsEmpty()
    {
        string source = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "source"), 3);
        ProcessRunner.RunGit(source, "tag", "v1.0.0");

        string shallow = Path.Combine(_workspace.RootDirectory, "shallow");
        // --no-local is required here: for a plain local filesystem path, git's local-clone
        // optimization bypasses shallow-transfer logic entirely and --depth is silently ignored,
        // producing a full clone that would make this test worthless.
        ProcessResult cloneResult = ProcessRunner.RunGit(Path.GetTempPath(), "clone", "--quiet", "--no-local", "--depth", "1", source, shallow);
        cloneResult.ExitCode.Should().Be(0, "shallow clone should succeed.");
        ProcessRunner.GetGitOutput(shallow, "rev-parse", "--is-shallow-repository").Should().Be("true");

        string testApp = _workspace.CopyCurrentProjectFiles(shallow);

        ProcessResult result = ProcessRunner.RunDotnet(testApp, "build");
        AssertBuildSucceeded(result, "the build against a shallow clone");
        result.Output.Should().NotContain("GITPROPS001");
        result.Output.Should().NotContain("GITPROPS002");
        AssertWarned(result, "GITPROPS006");

        Dictionary<string, string> properties = PropertiesFile.Read(DebugGitPropertiesFile(testApp));
        properties["git.total.commit.count"].Should().BeEmpty();
        properties["git.closest.tag.commit.count"].Should().BeEmpty();
    }

    /// <summary>
    /// GITPROPS006 (unlike GITPROPS001-005) never blocks generation - the shallow clone is still fully usable, just with two fields left empty (see
    /// <see cref="ShallowClone_LeavesCommitCountsEmpty" />). Confirms $(GitPropertiesEnableWarnings) downgrades it to an informational message the same way
    /// it does for the others.
    /// </summary>
    [Fact]
    public void ShallowClone_InfoWhenEnableWarningsFalse()
    {
        string source = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "source"), 1);

        string shallow = Path.Combine(_workspace.RootDirectory, "shallow");
        ProcessResult cloneResult = ProcessRunner.RunGit(Path.GetTempPath(), "clone", "--quiet", "--no-local", "--depth", "1", source, shallow);
        cloneResult.ExitCode.Should().Be(0, "shallow clone should succeed.");

        string testApp = _workspace.CopyCurrentProjectFiles(shallow);

        ProcessResult result = ProcessRunner.RunDotnet(testApp, "build", "-p:GitPropertiesEnableWarnings=false", "-v:normal");
        AssertBuildSucceeded(result, "build");
        AssertReportedAsInfoOnly(result, "GITPROPS006", "repository is a shallow clone");
    }

    [Fact]
    public void NonAscii_CommitDataRendersCorrectly()
    {
        string repository = Path.Combine(_workspace.RootDirectory, "repo");
        Directory.CreateDirectory(repository);
        ProcessRunner.RunGit(repository, "init", "--quiet", "--initial-branch=main", ".");
        // \u-escaped rather than literal, so this source file itself stays plain ASCII: renders as accented Latin-1
        // supplement letters plus the trailing three characters of "commit", spelled out in Japanese (CJK).
        const string nonAsciiUserName = "\u00DCn\u00EFc\u00F6d\u00E9 T\u00EBst";
        const string nonAsciiCommitMessage = "\u00DCn\u00EFc\u00F6d\u00E9 t\u00EBst commit \u65E5\u672C\u8A9E";

        ProcessRunner.RunGit(repository, "config", "user.name", nonAsciiUserName);
        ProcessRunner.RunGit(repository, "config", "user.email", "test@example.com");
        File.WriteAllText(Path.Combine(repository, ".gitignore"), "bin/\r\nobj/\r\n");
        File.WriteAllText(Path.Combine(repository, "file.txt"), "content");
        ProcessRunner.RunGit(repository, "add", "-A");
        ProcessRunner.RunGit(repository, "commit", "--quiet", "-m", nonAsciiCommitMessage);

        string testApp = _workspace.CopyCurrentProjectFiles(repository);

        ProcessResult result = ProcessRunner.RunDotnet(testApp, "build");
        AssertBuildSucceeded(result, "build");

        Dictionary<string, string> properties = PropertiesFile.Read(DebugGitPropertiesFile(testApp));
        properties["git.commit.user.name"].Should().Be(nonAsciiUserName);
        properties["git.commit.message.short"].Should().Be(nonAsciiCommitMessage);
    }

    [Fact]
    public void MultiProject_SharesCache()
    {
        string repository = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "repo"), 2);

        // Two independent projects at the repo root, each with their own ProjectReference/Import
        // pointing at the SAME sibling Steeltoe.Management.GitProperties.Build copy (CreateSyntheticRepo
        // already placed TestApp there; reuse that exact relative layout for ProjA/ProjB by placing them
        // at the repo root too, siblings of TestApp and "src").
        GitPropertiesTestWorkspace.WriteAppProject(repository, "ProjA");
        GitPropertiesTestWorkspace.WriteAppProject(repository, "ProjB");

        string projA = Path.Combine(repository, "ProjA");
        string projB = Path.Combine(repository, "ProjB");

        ProcessResult resultA = ProcessRunner.RunDotnet(projA, "build", "-v:detailed");
        AssertBuildSucceeded(resultA, "ProjA build");

        resultA.Output.Should().Contain("git.properties: generating shared cache",
            "ProjA (first to build) should be the one that actually generates the shared cache.");

        string cacheFile = Path.Combine(repository, "obj", "_GitProperties", "git.properties.cache");
        File.Exists(cacheFile).Should().BeTrue("ProjA's build should have generated the shared cache.");
        DateTime cacheWriteTimeAfterA = File.GetLastWriteTimeUtc(cacheFile);

        // Ensure a rewritten file would get a detectably different modified-time.
        Thread.Sleep(1100);

        ProcessResult resultB = ProcessRunner.RunDotnet(projB, "build", "-v:detailed");
        AssertBuildSucceeded(resultB, "ProjB build");
        resultB.Output.Should().NotContain("git.properties: generating shared cache", "ProjB should reuse ProjA's cache instead of regenerating it.");

        File.GetLastWriteTimeUtc(cacheFile).Should().Be(cacheWriteTimeAfterA, "ProjB must not have rewritten the shared cache file.");

        Dictionary<string, string> propertiesA = PropertiesFile.Read(DebugGitPropertiesFile(projA));
        Dictionary<string, string> propertiesB = PropertiesFile.Read(DebugGitPropertiesFile(projB));
        propertiesB["git.commit.id"].Should().Be(propertiesA["git.commit.id"]);
    }

    /// <summary>
    /// A single multi-targeted project (current TFM plus the one immediately before it - see <see cref="TestPaths.MultiTargetTestFrameworks" />) is a
    /// different sharing scenario than <see cref="MultiProject_SharesCache" />: MSBuild builds a multi-targeted project's inner TFMs concurrently by default
    /// (unlike the two sequential "dotnet build" invocations that test uses), which is exactly the race
    /// GenerateGitPropertiesCacheTask.TryGenerateAndWriteCache's cross-process lock exists to handle. Also guards against the regression that fix's first
    /// (wrong) attempt introduced: tagging the current commit invalidates the cache without changing the commit ID, so a naive "does the cache already
    /// reflect this commit" freshness check would wrongly skip regenerating it.
    /// </summary>
    [Fact]
    public void MultiTargetedProject_SharesCacheAcrossTargetFrameworks()
    {
        string repository = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "repo"), 1);
        string testApp = GitPropertiesTestWorkspace.WriteAppProject(repository, "MultiTargetApp", TestPaths.MultiTargetTestFrameworks);
        string[] frameworks = TestPaths.MultiTargetTestFrameworks.Split(';');

        ProcessResult result1 = ProcessRunner.RunDotnet(testApp, "build");
        AssertBuildSucceeded(result1, "multi-targeted build");

        string expectedCommitId = ProcessRunner.GetGitOutput(repository, "rev-parse", "HEAD");
        Dictionary<string, string>[] propertiesBefore = ReadPropertiesForEachFramework(testApp, frameworks);

        foreach (Dictionary<string, string> properties in propertiesBefore)
        {
            properties["git.commit.id"].Should().Be(expectedCommitId);
            properties["git.tags"].Should().BeEmpty();
        }

        ProcessResult tagResult = ProcessRunner.RunGit(repository, "tag", "v1.0.0");
        tagResult.ExitCode.Should().Be(0, "creating the test tag should succeed.");

        ProcessResult result2 = ProcessRunner.RunDotnet(testApp, "build");
        AssertBuildSucceeded(result2, "second multi-targeted build");

        Dictionary<string, string>[] propertiesAfter = ReadPropertiesForEachFramework(testApp, frameworks);

        foreach (Dictionary<string, string> properties in propertiesAfter)
        {
            properties["git.tags"].Should().Be("v1.0.0", "both target frameworks must observe the new tag, even though the commit it points at didn't change.");
        }
    }

    [Fact]
    public void WriteToProjectDirectory_DefaultsToOff()
    {
        string repository = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "repo"), 1);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult result = ProcessRunner.RunDotnet(testApp, "build");
        AssertBuildSucceeded(result, "build");

        File.Exists(FallbackFile(testApp)).Should().BeFalse("the fallback file must not be written into the project directory unless explicitly opted into.");
    }

    [Fact]
    public void WriteToProjectDirectory_CreatesFallbackFile_OnBuild()
    {
        string repository = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "repo"), 1, true);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult result = ProcessRunner.RunDotnet(testApp, "build", "-p:GitPropertiesWriteToProjectDirectory=true");
        AssertBuildSucceeded(result, "build with GitPropertiesWriteToProjectDirectory=true");

        result.Output.Should()
            .Contain($"git.properties: writing fallback copy to '{FallbackFile(testApp)}' for project '{GitPropertiesTestWorkspace.TestAppProjectName}'.");

        File.Exists(FallbackFile(testApp)).Should().BeTrue("the fallback file should have been written next to the .csproj.");

        Dictionary<string, string> fallbackProperties = PropertiesFile.Read(FallbackFile(testApp));
        Dictionary<string, string> outputProperties = PropertiesFile.Read(DebugGitPropertiesFile(testApp));
        fallbackProperties.Should().BeEquivalentTo(outputProperties, "the fallback file must carry the exact same content as the live build output.");

        ProcessRunner.GetGitOutput(repository, "status", "--porcelain").Should()
            .BeEmpty("the fallback file is gitignored, so it must not show up as an untracked change.");

        // A gitignored fallback file left over from the first build must not itself make a LATER build see the tree as dirty.
        ProcessResult secondResult = ProcessRunner.RunDotnet(testApp, "build", "-p:GitPropertiesWriteToProjectDirectory=true");
        AssertBuildSucceeded(secondResult, "second build");

        PropertiesFile.Read(DebugGitPropertiesFile(testApp))["git.dirty"].Should().Be("false",
            "the gitignored fallback file from the first build must not make a later build see the tree as dirty.");
    }

    /// <summary>
    /// The negative counterpart to <see cref="WriteToProjectDirectory_CreatesFallbackFile_OnBuild" /> - proves the README's ".gitignore this file" warning
    /// is describing a real consequence, not a hypothetical one: deliberately uses a repository WITHOUT the fallback file gitignored, so the file the first
    /// build writes is left behind as a genuine untracked change - permanently flipping git.dirty to "true" on every later build, even though nothing about
    /// the actually-tracked source changed in between.
    /// </summary>
    [Fact]
    public void FallbackFile_WithoutGitignore_MakesLaterBuildsAppearDirty()
    {
        string repository = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "repo"), 1);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult firstResult = ProcessRunner.RunDotnet(testApp, "build", "-p:GitPropertiesWriteToProjectDirectory=true");
        AssertBuildSucceeded(firstResult, "first build, which writes the (not yet gitignored) fallback file");

        ProcessRunner.GetGitOutput(repository, "status", "--porcelain").Should()
            .NotBeEmpty("the freshly-written, ungitignored fallback file should show up as an untracked change.");

        ProcessResult secondResult = ProcessRunner.RunDotnet(testApp, "build", "-p:GitPropertiesWriteToProjectDirectory=true");
        AssertBuildSucceeded(secondResult, "second build");

        PropertiesFile.Read(DebugGitPropertiesFile(testApp))["git.dirty"].Should().Be("true",
            "the ungitignored fallback file left over from the first build makes every later build see the tree as dirty.");
    }

    /// <summary>
    /// "dotnet publish" runs its own compile/composition steps internally regardless of whether "dotnet build" ran first - this guards against the fallback
    /// file only being written along the "build" target chain and silently never firing when publish is the very first command run against a fresh checkout
    /// (a common real-world pattern: `dotnet publish` directly, without a separate build step). Runs with $(GitPropertiesEnableWarnings) at its default
    /// (enabled) setting to confirm nothing about the fallback-writing path implicitly depends on warnings being suppressed - since a real .git repository
    /// is available here, nothing should be skipped (and no GITPROPS0xx code should appear) regardless of that setting.
    /// </summary>
    [Fact]
    public void WriteToProjectDirectory_CreatesFallbackFile_OnPublish()
    {
        string repository = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "repo"), 1, true);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult result =
            ProcessRunner.RunDotnet(testApp, "publish", "-p:GitPropertiesWriteToProjectDirectory=true", "-p:GitPropertiesEnableWarnings=true");

        AssertBuildSucceeded(result, "publish with GitPropertiesWriteToProjectDirectory=true, without an upfront build");

        result.Output.Should().NotContain("GITPROPS0",
            "nothing should be skipped, and no fallback should be needed, when a real .git repository is available.");

        File.Exists(FallbackFile(testApp)).Should().BeTrue("the fallback file should have been written next to the .csproj, even for a bare publish.");

        Dictionary<string, string> fallbackProperties = PropertiesFile.Read(FallbackFile(testApp));
        Dictionary<string, string> publishedProperties = PropertiesFile.Read(ReleasePublishGitPropertiesFile(testApp));
        fallbackProperties.Should().BeEquivalentTo(publishedProperties, "the fallback file must carry the exact same content as the published output.");

        ProcessRunner.GetGitOutput(repository, "status", "--porcelain").Should()
            .BeEmpty("the fallback file is gitignored, so it must not show up as an untracked change.");
    }

    /// <summary>
    /// Guards against a stale fallback file (left over from some earlier build) ever shadowing live generation - the fallback file must only ever be used as
    /// a last resort, never preferred over a real, currently-usable .git repository.
    /// </summary>
    [Fact]
    public void FallbackFile_Ignored_WhenLiveGitAvailable()
    {
        string repository = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "repo"), 1, true);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        File.WriteAllLines(FallbackFile(testApp), ["git.commit.id=deadbeefdeadbeefdeadbeefdeadbeefdeadbeef"]);

        ProcessResult result = ProcessRunner.RunDotnet(testApp, "build", "-v:detailed");
        AssertBuildSucceeded(result, "build with a stale fallback file present alongside a real .git repository");
        result.Output.Should().NotContain("using pre-generated fallback file", "the fallback notice must not appear when live generation actually ran.");

        Dictionary<string, string> properties = PropertiesFile.Read(DebugGitPropertiesFile(testApp));
        properties["git.commit.id"].Should().Be(ProcessRunner.GetGitOutput(repository, "rev-parse", "HEAD"));
    }

    /// <summary>
    /// End-to-end simulation of the scenario that motivated $(GitPropertiesWriteToProjectDirectory) in the first place: `cf push` using the
    /// dotnet_core_buildpack directly from source, which strips ".git" from the pushed tree unconditionally (see SimulateSourcePush) - meaning live
    /// generation can never run for that push, ever. A pre-generated fallback file (produced by an earlier LOCAL build, where .git was available) must ride
    /// along in the pushed source tree and get picked up, ending up in the published output exactly as if it had been generated live.
    /// </summary>
    [Fact]
    public void FallbackFile_UsedWhenNoGitAvailable()
    {
        string repository = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "repo"), 2, true);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult fallbackResult = ProcessRunner.RunDotnet(testApp, "build", "-p:GitPropertiesWriteToProjectDirectory=true");
        AssertBuildSucceeded(fallbackResult, "the local build that produces the fallback file");
        Dictionary<string, string> fallbackProperties = PropertiesFile.Read(FallbackFile(testApp));
        fallbackProperties["git.dirty"].Should().Be("false", "the gitignored fallback file must not make its own producing build see the tree as dirty.");

        string pushedRoot = GitPropertiesTestWorkspace.SimulateSourcePush(repository, Path.Combine(_workspace.RootDirectory, "pushed"));
        string pushedApp = Path.Combine(pushedRoot, GitPropertiesTestWorkspace.TestAppProjectName);
        Directory.Exists(Path.Combine(pushedRoot, ".git")).Should().BeFalse("the simulated push must not carry '.git' along, matching cf push's own default.");
        File.Exists(FallbackFile(pushedApp)).Should().BeTrue("the fallback git.properties must have survived the simulated push.");

        ProcessResult publishResult = ProcessRunner.RunDotnet(pushedApp, "publish", "-v:detailed");
        AssertBuildSucceeded(publishResult, "publish with no usable .git repository present");
        publishResult.Output.Should().NotContain("GITPROPS001", "the fallback file should suppress the usual no-.git diagnostic entirely.");

        publishResult.Output.Should()
            .Contain("using pre-generated fallback file", "using the fallback should still be traceable, so it's never silently stale.");

        Dictionary<string, string> publishedProperties = PropertiesFile.Read(ReleasePublishGitPropertiesFile(pushedApp));
        publishedProperties.Should().BeEquivalentTo(fallbackProperties, "the fallback-produced output must exactly match the pre-generated fallback content.");
    }

    /// <summary>
    /// The stable, documented entry point for step 1 of the "Recommended cf push workflow" (see PackageReadme.md) - confirms it actually produces a usable
    /// fallback file, and that doing so never compiles anything (the whole reason to prefer it over a full "dotnet build" before a source push).
    /// </summary>
    [Fact]
    public void WriteGitPropertiesFallbackFile_ProducesFallbackFile_WithoutCompiling()
    {
        string repository = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "repo"), 1, true);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult result = ProcessRunner.RunDotnet(testApp, "build", "-t:WriteGitPropertiesFallbackFile");
        AssertBuildSucceeded(result, "build -t:WriteGitPropertiesFallbackFile");

        File.Exists(FallbackFile(testApp)).Should().BeTrue("the fallback file should have been written next to the .csproj.");
        Dictionary<string, string> properties = PropertiesFile.Read(FallbackFile(testApp));
        properties["git.commit.id"].Should().Be(ProcessRunner.GetGitOutput(repository, "rev-parse", "HEAD"));

        // "bin\Debug\<TFM>\publish" gets created as empty, routine SDK scaffolding even here (PrepareForPublish's own setup) - checking for the absence
        // of the compiled assembly itself, not just the bin directory, is what actually proves no compilation happened.
        File.Exists(Path.Combine(testApp, "bin", "Debug", TestPaths.TestAppTargetFramework, $"{GitPropertiesTestWorkspace.TestAppProjectName}.dll")).Should()
            .BeFalse("this target must never compile the project - that's the whole point of using it instead of a full build before a source push.");
    }

    /// <summary>
    /// End-to-end simulation of the actual documented workflow - the lightweight-target equivalent of <see cref="FallbackFile_UsedWhenNoGitAvailable" />
    /// (which uses a full build instead): produce the fallback file via
    /// <c>
    /// WriteGitPropertiesFallbackFile
    /// </c>
    /// , simulate a source-based `cf push`, and confirm the server-side publish still picks it up correctly.
    /// </summary>
    [Fact]
    public void WriteGitPropertiesFallbackFile_ThenSimulatedPush_ServerPublishUsesIt()
    {
        string repository = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "repo"), 2, true);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult writeResult = ProcessRunner.RunDotnet(testApp, "build", "-t:WriteGitPropertiesFallbackFile");
        AssertBuildSucceeded(writeResult, "build -t:WriteGitPropertiesFallbackFile");
        Dictionary<string, string> fallbackProperties = PropertiesFile.Read(FallbackFile(testApp));

        string pushedRoot = GitPropertiesTestWorkspace.SimulateSourcePush(repository, Path.Combine(_workspace.RootDirectory, "pushed"));
        string pushedApp = Path.Combine(pushedRoot, GitPropertiesTestWorkspace.TestAppProjectName);
        File.Exists(FallbackFile(pushedApp)).Should().BeTrue("the fallback git.properties must have survived the simulated push.");

        ProcessResult publishResult = ProcessRunner.RunDotnet(pushedApp, "publish", "-v:detailed");
        AssertBuildSucceeded(publishResult, "publish with no usable .git repository present");

        publishResult.Output.Should()
            .Contain("using pre-generated fallback file", "using the fallback should still be traceable, so it's never silently stale.");

        Dictionary<string, string> publishedProperties = PropertiesFile.Read(ReleasePublishGitPropertiesFile(pushedApp));
        publishedProperties.Should().BeEquivalentTo(fallbackProperties, "the fallback-produced output must exactly match the pre-generated fallback content.");
    }

    /// <summary>
    /// "--no-restore" must work the same way for this target as for any other build invocation - it only requires that restore already happened at least
    /// once, same as a normal build.
    /// </summary>
    [Fact]
    public void WriteGitPropertiesFallbackFile_WorksWithNoRestore()
    {
        string repository = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "repo"), 1, true);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult restoreResult = ProcessRunner.RunDotnet(testApp, "restore");
        AssertBuildSucceeded(restoreResult, "restore");

        ProcessResult result = ProcessRunner.RunDotnet(testApp, "build", "--no-restore", "-t:WriteGitPropertiesFallbackFile");
        AssertBuildSucceeded(result, "build --no-restore -t:WriteGitPropertiesFallbackFile");

        File.Exists(FallbackFile(testApp)).Should().BeTrue();
    }

    /// <summary>
    /// Documents/guards the one real caveat called out in PackageReadme.md: this target never produces real build output, so a local "dotnet publish
    /// --no-build" afterward must fail - there is nothing compiled to publish. If this target's own implementation ever accidentally started producing
    /// compiled output (defeating its "lightweight" purpose), this test would start failing for the opposite reason (publish --no-build would start
    /// succeeding) - a signal to revisit the target, not just delete this test.
    /// </summary>
    [Fact]
    public void WriteGitPropertiesFallbackFile_ThenPublishNoBuild_Fails()
    {
        string repository = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "repo"), 1, true);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult writeResult = ProcessRunner.RunDotnet(testApp, "build", "-t:WriteGitPropertiesFallbackFile");
        AssertBuildSucceeded(writeResult, "build -t:WriteGitPropertiesFallbackFile");

        ProcessResult publishResult = ProcessRunner.RunDotnet(testApp, "publish", "--no-build");

        publishResult.ExitCode.Should().NotBe(0,
            "publishing --no-build after only writing the fallback file (no real build ever ran) must fail - there is no compiled output to publish.");
    }

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
    public void NuGetPackage_ConsumedViaPackageReference_GeneratesGitProperties()
    {
        string repository = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "repo"), 1);

        string feedDirectory = _workspace.PackGitPropertiesBuildToFeed();

        string[] nuPkgFiles = Directory.GetFiles(feedDirectory, $"{TestPaths.PackageId}.*.nupkg");
        nuPkgFiles.Should().ContainSingle("packing should produce exactly one .nupkg.");

        Match versionMatch = NuPkgVersionRegex.Match(Path.GetFileName(nuPkgFiles[0]));
        versionMatch.Success.Should().BeTrue("the .nupkg file name should embed the package version.");
        string packageVersion = versionMatch.Groups[1].Value;

        string consumerDirectory = Path.Combine(repository, "Consumer");
        GitPropertiesTestWorkspace.CreatePackageConsumerProject(consumerDirectory, packageVersion);
        GitPropertiesTestWorkspace.WriteIsolatedNuGetConfig(Path.Combine(consumerDirectory, "nuget.config"), feedDirectory);

        string isolatedPackagesPath = Path.Combine(_workspace.RootDirectory, "isolated-packages");
        ProcessResult result = ProcessRunner.RunDotnet(consumerDirectory, "build", $"-p:RestorePackagesPath={isolatedPackagesPath}");
        AssertBuildSucceeded(result, "the build of a project consuming Steeltoe.Management.GitProperties.Build via PackageReference");

        result.Output.Should().Contain("0 Warning(s)",
            "a real package consumer should see no in-process task-loading fallback warning or any other diagnostic.");

        // NuGet always lowercases the package ID for the on-disk global-packages-folder layout - this isn't
        // an arbitrary case normalization, so ToUpperInvariant() (as generally preferred) would look here for
        // a folder that NuGet never creates.
#pragma warning disable S4040
        string lowerCasePackageId = TestPaths.PackageId.ToLowerInvariant();
#pragma warning restore S4040

        Directory.Exists(Path.Combine(isolatedPackagesPath, lowerCasePackageId, packageVersion)).Should()
            .BeTrue("the package should restore into the isolated path, never the machine-wide global-packages cache.");

        Dictionary<string, string> properties = PropertiesFile.Read(DebugGitPropertiesFile(consumerDirectory));
        properties["git.commit.id"].Should().Be(ProcessRunner.GetGitOutput(repository, "rev-parse", "HEAD"));
    }

    /// <summary>
    /// The default case for the overwhelming majority of projects in a large solution (a class library, a test project, anything without a consuming package
    /// anywhere in its resolved dependency graph): generation is skipped entirely, without needing an explicit opt-out, and without breaking the build. A
    /// real git repository is deliberately present here (unlike NoGit_WarnsByDefault) to prove the smart default - not "no .git found" - is what causes the
    /// skip.
    /// </summary>
    [Fact]
    public void SmartDefault_SkipsGeneration_WhenNoConsumingPackageReference()
    {
        string repository = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "repo"), 1);
        string testApp = GitPropertiesTestWorkspace.WriteAppProject(repository, GitPropertiesTestWorkspace.TestAppProjectName, generateGitProperties: null);

        ProcessResult result = ProcessRunner.RunDotnet(testApp, "build", "-v:detailed");
        AssertBuildSucceeded(result, "build with no consuming-package reference and $(GenerateGitProperties) left at its smart default");
        // Not a numbered GITPROPS0xx code - this is plain internal trace output, not a diagnosable outcome (see the .targets file's own comment on it).
        result.Output.Should().Contain("git.properties generation skipped: no reference to");
        AssertNoGitPropertiesGenerated(testApp);
    }

    /// <summary>
    /// The positive counterpart to <see cref="SmartDefault_SkipsGeneration_WhenNoConsumingPackageReference" />: a project referencing the real default
    /// consuming package ID (Steeltoe.Management.Endpoint) gets git.properties generated with no explicit $(GenerateGitProperties) needed. Uses a minimal
    /// stand-in project with that exact name/PackageId (see WriteDummyDependencyProject's remarks) rather than the real, large Endpoint project, so this
    /// test stays fast and fully offline.
    /// </summary>
    [Fact]
    public void SmartDefault_GeneratesGitProperties_WhenConsumingPackageReferenced()
    {
        string repository = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "repo"), 1);
        const string consumingPackageStandInName = "Steeltoe.Management.Endpoint";
        GitPropertiesTestWorkspace.WriteDummyDependencyProject(repository, consumingPackageStandInName);

        string testApp = GitPropertiesTestWorkspace.WriteAppProject(repository, GitPropertiesTestWorkspace.TestAppProjectName, generateGitProperties: null,
            extraItemGroupContent: $"""<ProjectReference Include="..\{consumingPackageStandInName}\{consumingPackageStandInName}.csproj" />""");

        ProcessResult result = ProcessRunner.RunDotnet(testApp, "build");
        AssertBuildSucceeded(result, "build with a Steeltoe.Management.Endpoint reference and $(GenerateGitProperties) left at its smart default");

        Dictionary<string, string> properties = PropertiesFile.Read(DebugGitPropertiesFile(testApp));
        properties["git.commit.id"].Should().Be(ProcessRunner.GetGitOutput(repository, "rev-parse", "HEAD"));
    }

    /// <summary>
    /// Proves $(GitPropertiesConsumingPackageIds) is genuinely overridable - for consumers of this package who don't use Steeltoe.Management.Endpoint at all
    /// (e.g. a hand-rolled /info endpoint reading git.properties directly), so the smart default isn't hardcoded away from them.
    /// </summary>
    [Fact]
    public void SmartDefault_Override_DetectsCustomPackageIds()
    {
        string repository = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "repo"), 1);
        const string customPackageId = "Contoso.Actuators";
        GitPropertiesTestWorkspace.WriteDummyDependencyProject(repository, customPackageId);

        string testApp = GitPropertiesTestWorkspace.WriteAppProject(repository, GitPropertiesTestWorkspace.TestAppProjectName, generateGitProperties: null,
            extraItemGroupContent: $"""<ProjectReference Include="..\{customPackageId}\{customPackageId}.csproj" />""");

        ProcessResult result = ProcessRunner.RunDotnet(testApp, "build", $"-p:GitPropertiesConsumingPackageIds={customPackageId}");
        AssertBuildSucceeded(result, "build with a custom $(GitPropertiesConsumingPackageIds) matching a referenced project");

        Dictionary<string, string> properties = PropertiesFile.Read(DebugGitPropertiesFile(testApp));
        properties["git.commit.id"].Should().Be(ProcessRunner.GetGitOutput(repository, "rev-parse", "HEAD"));
    }

    /// <summary>
    /// Guards against a regression to a naive substring match (e.g. "IndexOf(id + "/")" without also requiring the match to be a whole library key) - a
    /// project referencing only "Some2" (never "Some" itself) must NOT be detected when $(GitPropertiesConsumingPackageIds) is configured as "Some", even
    /// though "Some2" starts with "Some". Proves DetectConsumingPackageReferenceTask compares whole package IDs, not prefixes.
    /// </summary>
    [Fact]
    public void SmartDefault_Override_DoesNotMatchPackageIdAsPrefix()
    {
        string repository = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "repo"), 1);
        const string longerPackageId = "Some2";
        GitPropertiesTestWorkspace.WriteDummyDependencyProject(repository, longerPackageId);

        string testApp = GitPropertiesTestWorkspace.WriteAppProject(repository, GitPropertiesTestWorkspace.TestAppProjectName, generateGitProperties: null,
            extraItemGroupContent: $"""<ProjectReference Include="..\{longerPackageId}\{longerPackageId}.csproj" />""");

        ProcessResult result = ProcessRunner.RunDotnet(testApp, "build", "-p:GitPropertiesConsumingPackageIds=Some");
        AssertBuildSucceeded(result, "build with a referenced package ('Some2') that is a superstring, not a match, of the configured ID ('Some')");
        AssertNoGitPropertiesGenerated(testApp);
    }

    /// <summary>
    /// Guards against a regression where MSBuild's required-parameter check for a Task string parameter treats an empty string the same as "not supplied":
    /// setting $(GitPropertiesConsumingPackageIds) to blank via a global property (e.g. "-p:GitPropertiesConsumingPackageIds=") reaches
    /// DetectConsumingPackageReferenceTask.PackageIds unchanged (global properties can't be reassigned by the project's own conditional default at
    /// ResolveGitPropertiesPaths above), so PackageIds must NOT be [Required] - it must instead behave exactly like "no configured ID happens to match",
    /// i.e. skip generation gracefully rather than fail the build with MSB4044.
    /// </summary>
    [Fact]
    public void SmartDefault_Override_EmptyPackageIdsViaGlobalProperty_SkipsGenerationGracefully()
    {
        string repository = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "repo"), 1);
        const string consumingPackageStandInName = "Steeltoe.Management.Endpoint";
        GitPropertiesTestWorkspace.WriteDummyDependencyProject(repository, consumingPackageStandInName);

        string testApp = GitPropertiesTestWorkspace.WriteAppProject(repository, GitPropertiesTestWorkspace.TestAppProjectName, generateGitProperties: null,
            extraItemGroupContent: $"""<ProjectReference Include="..\{consumingPackageStandInName}\{consumingPackageStandInName}.csproj" />""");

        ProcessResult result = ProcessRunner.RunDotnet(testApp, "build", "-p:GitPropertiesConsumingPackageIds=");
        AssertBuildSucceeded(result, "build with $(GitPropertiesConsumingPackageIds) explicitly cleared via a global property");
        AssertNoGitPropertiesGenerated(testApp);
    }

    /// <summary>
    /// A consumer's explicit choice must never be second-guessed by the smart default, in either direction - the negative direction (no reference, but
    /// explicitly forced on) is already exercised by every other test in this file, which all set $(GenerateGitProperties)=true explicitly via
    /// WriteAppProject's default. This covers the other direction: a consuming-package reference IS present (the smart default would say "generate"), but
    /// the consumer explicitly opted out anyway.
    /// </summary>
    [Fact]
    public void SmartDefault_ExplicitFalse_WinsOverDetectedConsumingPackageReference()
    {
        string repository = _workspace.CreateSyntheticRepo(Path.Combine(_workspace.RootDirectory, "repo"), 1);
        const string consumingPackageStandInName = "Steeltoe.Management.Endpoint";
        GitPropertiesTestWorkspace.WriteDummyDependencyProject(repository, consumingPackageStandInName);

        string testApp = GitPropertiesTestWorkspace.WriteAppProject(repository, GitPropertiesTestWorkspace.TestAppProjectName, generateGitProperties: null,
            extraItemGroupContent: $"""<ProjectReference Include="..\{consumingPackageStandInName}\{consumingPackageStandInName}.csproj" />""");

        ProcessResult result = ProcessRunner.RunDotnet(testApp, "build", "-p:GenerateGitProperties=false");
        AssertBuildSucceeded(result, "build with GenerateGitProperties explicitly set to false despite a consuming-package reference being present");
        AssertNoGitPropertiesGenerated(testApp);
    }

    private static Dictionary<string, string>[] ReadPropertiesForEachFramework(string projectDirectory, string[] frameworks)
    {
        return Array.ConvertAll(frameworks, framework => PropertiesFile.Read(Path.Combine(projectDirectory, "bin", "Debug", framework, "git.properties")));
    }

    private static string FallbackFile(string projectDirectory)
    {
        return Path.Combine(projectDirectory, "git.properties");
    }

    public void Dispose()
    {
        _workspace.Dispose();
    }

    private static void AssertBuildSucceeded(ProcessResult result, string action)
    {
        result.ExitCode.Should().Be(0, "{0} should succeed. Output:\n{1}", action, result.Output);
    }

    private static void AssertWarned(ProcessResult result, string code)
    {
        result.Output.Should().Contain($"warning {code}");
    }

    /// <summary>
    /// GitPropertiesEnableWarnings=false downgrades a diagnostic from a Warning to a plain informational message - with no code at all (see
    /// GenerateGitPropertiesCacheTask.ReportDiagnostic's remarks for why), and at Importance="Normal" rather than the default's "high", so it's visible at
    /// "-v:normal" but not in default build output.
    /// </summary>
    private static void AssertReportedAsInfoOnly(ProcessResult result, string code, string messageSnippet)
    {
        result.Output.Should().NotContain(code, "a downgraded message must never carry a code - only warnings do.");
        result.Output.Should().Contain(messageSnippet);
    }

    private static void AssertNoGitPropertiesGenerated(string projectDirectory)
    {
        File.Exists(DebugGitPropertiesFile(projectDirectory)).Should().BeFalse("no git.properties should be generated.");
    }

    private static string DebugGitPropertiesFile(string projectDirectory)
    {
        return Path.Combine(projectDirectory, "bin", "Debug", TestPaths.TestAppTargetFramework, "git.properties");
    }

    private static string ReleasePublishGitPropertiesFile(string projectDirectory)
    {
        return Path.Combine(projectDirectory, "bin", "Release", TestPaths.TestAppTargetFramework, "publish", "git.properties");
    }
}
