// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace Steeltoe.Management.GitProperties.Build.Test;

/// <summary>
/// An isolated temporary directory tree for a single test, cleaned up on Dispose - and the factory every test uses to create the
/// <see cref="GitRepository" />/<see cref="TestProject" /> instances its scenario needs, so no test has to combine a path under
/// <see cref="RootDirectory" /> itself. Deliberately avoids "gitprop" in its own name (in any casing) so a test's workspace path can never accidentally
/// satisfy an Assert.Contains/DoesNotContain check against a GITPROPS0xx diagnostic code in build output, which routinely echoes back the working
/// directory path.
/// </summary>
/// <remarks>
/// Constructed via <see cref="CreateAsync" /> rather than a public constructor: resolving the physical (symlink-free) root directory on macOS needs a
/// "pwd -P" subprocess, and a constructor can't await one.
/// </remarks>
internal sealed class GitPropertiesTestWorkspace : IDisposable
{
    /// <summary>
    /// The project name every dev-loop consumer test writes its own copy of Steeltoe.Management.GitProperties.Build against (see
    /// <see cref="TestProjectWriter.WriteAppProjectAsync" />) - shared so callers never retype it.
    /// </summary>
    public const string TestAppProjectName = "TestApp";

    public string RootDirectory { get; }

    private GitPropertiesTestWorkspace(string rootDirectory)
    {
        RootDirectory = rootDirectory;
    }

    public static async Task<GitPropertiesTestWorkspace> CreateAsync()
    {
        string rootDirectory = Path.Combine(Path.GetTempPath(), $"build-tasks-test_{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)[..8]}");
        Directory.CreateDirectory(rootDirectory);
        string physicalRootDirectory = await ResolvePhysicalPathAsync(rootDirectory);
        return new GitPropertiesTestWorkspace(physicalRootDirectory);
    }

    /// <summary>
    /// On macOS, $TMPDIR resolves through a symlink (/var -&gt; /private/var) that the OS silently canonicalizes away whenever a spawned process (git,
    /// dotnet, MSBuild) reports its own working directory - e.g. in "git.properties: writing..." diagnostic messages. Resolving once up front here keeps
    /// every path-based assertion in these tests (which compares against exactly that reported text) consistent with what a spawned process itself reports,
    /// instead of the un-resolved alias $TMPDIR itself returns.
    /// </summary>
    private static async Task<string> ResolvePhysicalPathAsync(string path)
    {
        if (!OperatingSystem.IsMacOS())
        {
            return path;
        }

        string output = await ProcessRunner.RunPwdAsync(path);
        return output.Trim();
    }

    public void Dispose()
    {
        // Set GITPROPERTIES_KEEP_TEST_WORKSPACES=1 to inspect a workspace after the run instead of
        // having it deleted here.
        if (Environment.GetEnvironmentVariable("GITPROPERTIES_KEEP_TEST_WORKSPACES") == "1")
        {
            return;
        }

        try
        {
            // git marks files under .git\objects (and packed clones) read-only on Windows, which
            // makes a plain recursive delete throw UnauthorizedAccessException - PowerShell's
            // Remove-Item -Force clears this automatically, but Directory.Delete does not.
            ClearReadOnlyAttributes(new DirectoryInfo(RootDirectory));
            Directory.Delete(RootDirectory, true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Best-effort cleanup only - a transiently locked file (e.g. an antivirus scan) must
            // not fail the test run.
        }
    }

    private static void ClearReadOnlyAttributes(DirectoryInfo directory)
    {
        foreach (FileInfo file in directory.GetFiles())
        {
            file.Attributes = FileAttributes.Normal;
        }

        foreach (DirectoryInfo subDirectory in directory.GetDirectories())
        {
            ClearReadOnlyAttributes(subDirectory);
        }
    }

    /// <summary>
    /// Combines a short, per-test name (e.g. "repo", "proj") with <see cref="RootDirectory" /> - the one remaining path-combining primitive every factory
    /// method below (and the occasional test that needs a workspace-scoped scratch path with no object of its own, e.g. an isolated NuGet packages folder)
    /// is built on.
    /// </summary>
    public string GetPath(string name)
    {
        return Path.Combine(RootDirectory, name);
    }

    /// <summary>
    /// A directory with no git repository at all, containing just a copy of the CURRENT Steeltoe.Management.GitProperties.Build source and its default
    /// TestApp - see <see cref="TestProjectWriter.CopyCurrentProjectFilesAsync" />. For tests that specifically cover the "no usable git repository above
    /// this project" diagnostics.
    /// </summary>
    public async Task<TestProject> CreateProjectDirectoryAsync(string name)
    {
        string directory = GetPath(name);
        Directory.CreateDirectory(directory);
        string appDirectory = await TestProjectWriter.CopyCurrentProjectFilesAsync(directory);
        return new TestProject(appDirectory, TestAppProjectName);
    }

    /// <summary>
    /// A freshly-initialized git repository with zero commits - "git init" only, no config/.gitignore/manufactured history. For tests that specifically
    /// cover the "repository has no commits yet" diagnostic, and any scenario that wants full manual control over its own git commands before adding
    /// projects (see <see cref="EmptyGitRepository.AddTestAppAsync" />).
    /// </summary>
    public async Task<EmptyGitRepository> CreateEmptyRepositoryAsync(string name)
    {
        string directory = GetPath(name);
        await GitRepositoryBuilder.InitializeEmptyAsync(directory);
        return new EmptyGitRepository(this, directory);
    }

    /// <summary>
    /// A brand-new, synthetic git repo with a controlled, minimal history (`git init` plus a handful of manufactured commits, via
    /// <see cref="GitRepositoryBuilder.InitializeAsync" />) plus the default TestApp - deliberately never a clone of this (large, real) repository, so the
    /// suite stays fast.
    /// </summary>
    /// <param name="name">
    /// The directory to initialize the repository in, relative to <see cref="RootDirectory" />.
    /// </param>
    /// <param name="commitCount">
    /// The number of manufactured commits to create before the project files are added.
    /// </param>
    /// <param name="gitignoreFallbackFile">
    /// Whether to also list "git.properties" in the repository's .gitignore, modeling the setup a real consumer of $(GitPropertiesWriteToProjectDirectory)
    /// must follow - see <see cref="GitRepositoryBuilder.SimulateSourcePush" /> for the complementary "not .cfignore'd" half of that same guidance. Defaults
    /// to false so most tests (which never write a fallback file into the project directory at all) aren't given a gitignore entry they don't exercise -
    /// only pass true for tests that specifically cover $(GitPropertiesWriteToProjectDirectory)/the fallback file, so a regression that accidentally wrote
    /// one in a test that doesn't expect it still shows up as an untracked file (and, transitively, as git.dirty=true) instead of being silently absorbed by
    /// a blanket ignore rule.
    /// </param>
    public async Task<GitRepository> CreateGitRepositoryAsync(string name, int commitCount, bool gitignoreFallbackFile = false)
    {
        string directory = GetPath(name);
        await GitRepositoryBuilder.InitializeAsync(directory, commitCount, gitignoreFallbackFile);
        TestProject testApp = await GitRepository.WriteDefaultTestAppAsync(directory);
        var repository = new GitRepository(this, directory, testApp);

        // Commit the project files too, so the synthetic repo starts clean (git.dirty=false)
        // unless a test deliberately makes a further change - otherwise every synthetic repo would
        // show git.dirty=true purely because of these untracked-but-just-added files.
        await GitRepositoryBuilder.CommitAllAsync(directory, "Add project files");
        return repository;
    }

    public Task<string> PackGitPropertiesBuildToFeedAsync()
    {
        return TestProjectWriter.PackGitPropertiesBuildToFeedAsync(RootDirectory);
    }

    public Task WriteIsolatedNuGetConfigAsync(TestProject project, string feedDirectory)
    {
        return TestProjectWriter.WriteIsolatedNuGetConfigAsync(Path.Combine(project.RootDirectory, "nuget.config"), feedDirectory);
    }
}
