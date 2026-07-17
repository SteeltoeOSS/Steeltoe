// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace Steeltoe.Management.GitProperties.Build.Test;

/// <summary>
/// An isolated temporary directory tree for a single test, cleaned up on Dispose - and the entry point every test uses to set up its own synthetic
/// project/repository layout. Owns only its own lifecycle plus the couple of methods that need its own <see cref="RootDirectory" />; the actual git-repo
/// mechanics live in <see cref="SyntheticGitRepositoryBuilder" /> and project/package file writing lives in <see cref="TestProjectWriter" /> - both are
/// exposed here as thin forwarding methods purely so every test can keep calling through
/// <c>
/// Workspace
/// </c>
/// /<see cref="GitPropertiesTestWorkspace" /> without needing to know which of the three classes actually implements a given piece. Deliberately avoids
/// "gitprop" in its own name (in any casing) so a test's workspace path can never accidentally satisfy an Assert.Contains/DoesNotContain check against a
/// GITPROPS0xx diagnostic code in build output, which routinely echoes back the working directory path.
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
    /// A brand-new, synthetic git repo with a controlled, minimal history (`git init` plus a handful of manufactured commits, via
    /// <see cref="SyntheticGitRepositoryBuilder.InitializeAsync" />) - deliberately never a clone of this (large, real) repository, so the suite stays fast.
    /// Returns the repository root (not the TestApp directory - callers combine "TestApp" themselves, mirroring how multi-project tests place additional
    /// sibling projects at the same root).
    /// </summary>
    /// <param name="destination">
    /// The directory to initialize the repository in.
    /// </param>
    /// <param name="commitCount">
    /// The number of manufactured commits to create before the project files are added.
    /// </param>
    /// <param name="gitignoreFallbackFile">
    /// Whether to also list "git.properties" in the repository's .gitignore, modeling the setup a real consumer of $(GitPropertiesWriteToProjectDirectory)
    /// must follow - see <see cref="SyntheticGitRepositoryBuilder.SimulateSourcePush" /> for the complementary "not .cfignore'd" half of that same guidance.
    /// Defaults to false so most tests (which never write a fallback file into the project directory at all) aren't given a gitignore entry they don't
    /// exercise - only pass true for tests that specifically cover $(GitPropertiesWriteToProjectDirectory)/the fallback file, so a regression that
    /// accidentally wrote one in a test that doesn't expect it still shows up as an untracked file (and, transitively, as git.dirty=true) instead of being
    /// silently absorbed by a blanket ignore rule.
    /// </param>
    public async Task<string> CreateSyntheticRepoAsync(string destination, int commitCount, bool gitignoreFallbackFile = false)
    {
        await SyntheticGitRepositoryBuilder.InitializeAsync(destination, commitCount, gitignoreFallbackFile);
        await TestProjectWriter.CopyCurrentProjectFilesAsync(destination);

        // Commit the project files too, so the synthetic repo starts clean (git.dirty=false)
        // unless a test deliberately makes a further change - otherwise every synthetic repo would
        // show git.dirty=true purely because of these untracked-but-just-added files.
        await SyntheticGitRepositoryBuilder.CommitAllAsync(destination, "Add project files");
        return destination;
    }
}
