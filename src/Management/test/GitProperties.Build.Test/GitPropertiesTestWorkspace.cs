// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace Steeltoe.Management.GitProperties.Build.Test;

/// <summary>
/// An isolated temporary directory tree for a single test, cleaned up on Dispose. Deliberately avoids "gitprop" in its own name (in any casing) so a
/// test's workspace path can never accidentally satisfy an Assert.Contains/DoesNotContain check against a GITPROPS0xx diagnostic code in build output,
/// which routinely echoes back the working directory path.
/// </summary>
internal sealed class GitPropertiesTestWorkspace : IDisposable
{
    /// <summary>
    /// The project name every dev-loop consumer test writes its own copy of Steeltoe.Management.GitProperties.Build against (see
    /// <see cref="WriteAppProject" />) - shared so callers never retype it.
    /// </summary>
    public const string TestAppProjectName = "TestApp";

    private static readonly HashSet<string> SimulatedPushExcludedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        "bin",
        "obj"
    };

    public string RootDirectory { get; }

    public GitPropertiesTestWorkspace()
    {
        RootDirectory = Path.Combine(Path.GetTempPath(), $"build-tasks-test_{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)[..8]}");
        Directory.CreateDirectory(RootDirectory);
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
    /// Copies the CURRENT Steeltoe.Management.GitProperties.Build source (csproj + all .cs files + the targets file + SourceCheckout.txt) into
    /// "src\Management\src\GitProperties.Build" under the given repository root - the exact relative depth its own csproj's "shared.props" import expects.
    /// Copying the marker file matters: without it, $(GitPropertiesTaskHost) in Steeltoe.Management.GitProperties.Build.targets would (correctly, but
    /// unhelpfully for these tests) detect "packaged" consumption instead of the dev loop this is meant to simulate, silently skipping the TaskHostFactory
    /// path every other test here relies on.
    /// </summary>
    private static void CopyGitPropertiesBuildSource(string repoRootDestination)
    {
        string destination = Path.Combine(repoRootDestination, TestPaths.GitPropertiesBuildRelativePath);
        Directory.CreateDirectory(Path.Combine(destination, "build"));
        File.Copy(TestPaths.GitPropertiesBuildProjectFile, Path.Combine(destination, Path.GetFileName(TestPaths.GitPropertiesBuildProjectFile)), true);
        File.Copy(TestPaths.TargetsFile, Path.Combine(destination, "build", Path.GetFileName(TestPaths.TargetsFile)), true);
        File.Copy(TestPaths.SourceCheckoutMarkerFile, Path.Combine(destination, Path.GetFileName(TestPaths.SourceCheckoutMarkerFile)), true);

        foreach (string sourceFile in Directory.GetFiles(TestPaths.GitPropertiesBuildDirectory, "*.cs"))
        {
            File.Copy(sourceFile, Path.Combine(destination, Path.GetFileName(sourceFile)), true);
        }
    }

    /// <summary>
    /// Copies the handful of root-level Steeltoe build infrastructure files that Steeltoe.Management.GitProperties.Build.csproj's own shared.props import
    /// chain needs (versioning, packaging defaults, analyzers), so a synthetic test repo - which has none of Steeltoe's other real projects - still
    /// evaluates the same properties a real build inside the repository would.
    /// </summary>
    private static void CopySharedBuildInfrastructure(string repoRootDestination)
    {
        Directory.CreateDirectory(repoRootDestination);

        foreach (string fileName in TestPaths.SharedBuildInfrastructureFiles)
        {
            File.Copy(Path.Combine(TestPaths.RepositoryRoot, fileName), Path.Combine(repoRootDestination, fileName), true);
        }
    }

    /// <summary>
    /// Writes a minimal exe project named <paramref name="projectName" /> under the given repository root, referencing
    /// Steeltoe.Management.GitProperties.Build the way a same-solution project (dev loop) would: a ReferenceOutputAssembly="false" ProjectReference (just to
    /// order the build) plus an explicit Import of the .targets file - the NuGet "build\{PackageId}.targets" auto-import convention only kicks in for a real
    /// PackageReference consumer (see NuGetPackage_ConsumedViaPackageReference_GeneratesGitProperties for that). Single-targeted at
    /// <see cref="TestPaths.TestAppTargetFramework" /> unless <paramref name="targetFrameworks" /> is given, in which case the project multi-targets that
    /// semicolon-separated list instead (see <see cref="TestPaths.MultiTargetTestFrameworks" />).
    /// </summary>
    /// <param name="repoRootDestination">
    /// The directory to write the project under - typically a repository root returned by <see cref="CreateSyntheticRepo" />.
    /// </param>
    /// <param name="projectName">
    /// The project's name - also used as its directory and file name.
    /// </param>
    /// <param name="targetFrameworks">
    /// A semicolon-separated list of target frameworks to multi-target, or null for a single-targeted project at
    /// <see cref="TestPaths.TestAppTargetFramework" />.
    /// </param>
    /// <param name="generateGitProperties">
    /// Emits an explicit $(GenerateGitProperties) override when non-null - true for almost every test here, since they exist to test generation itself, not
    /// the smart default that decides whether it runs at all. Pass null (letting the smart default apply) only for tests that specifically cover
    /// DetectConsumingPackageReferenceTask/$(GitPropertiesConsumingPackageIds) - see <see cref="WriteDummyDependencyProject" />.
    /// </param>
    /// <param name="extraItemGroupContent">
    /// Extra raw XML inserted into the same &lt;ItemGroup&gt; as the ProjectReference above - currently only used to add a second, normal (not
    /// ReferenceOutputAssembly="false") ProjectReference to a <see cref="WriteDummyDependencyProject" /> stand-in, so it actually participates in restore
    /// and shows up in this project's own project.assets.json (see DetectConsumingPackageReferenceTask's remarks for why that distinction matters).
    /// </param>
    public static string WriteAppProject(string repoRootDestination, string projectName, string? targetFrameworks = null, bool? generateGitProperties = true,
        string? extraItemGroupContent = null)
    {
        string appDirectory = Path.Combine(repoRootDestination, projectName);
        Directory.CreateDirectory(appDirectory);

        string targetFrameworkElement = targetFrameworks == null
            ? $"<TargetFramework>{TestPaths.TestAppTargetFramework}</TargetFramework>"
            : $"<TargetFrameworks>{targetFrameworks}</TargetFrameworks>";

        string generateGitPropertiesElement = string.Empty;

        if (generateGitProperties != null)
        {
            string generateGitPropertiesValue = generateGitProperties.Value ? "true" : "false";
            generateGitPropertiesElement = $"<GenerateGitProperties>{generateGitPropertiesValue}</GenerateGitProperties>";
        }

        string projectContent = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                {targetFrameworkElement}
                {generateGitPropertiesElement}
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>

              <ItemGroup>
                <ProjectReference Include="../{TestPaths.GitPropertiesBuildRelativePath}/{Path.GetFileName(TestPaths.GitPropertiesBuildProjectFile)}">
                  <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
                </ProjectReference>
                {extraItemGroupContent}
              </ItemGroup>

              <Import Project="$(MSBuildThisFileDirectory)../{TestPaths.GitPropertiesBuildRelativePath}/build/{Path.GetFileName(TestPaths.TargetsFile)}" />
            </Project>
            """;

        File.WriteAllText(Path.Combine(appDirectory, $"{projectName}.csproj"), projectContent);

        File.WriteAllText(Path.Combine(appDirectory, "Program.cs"), """
        Console.WriteLine("Hello, World!");
        """);

        return appDirectory;
    }

    /// <summary>
    /// Writes a minimal, do-nothing class library project - used only by the smart-default detection tests as a stand-in for a real dependency (e.g.
    /// Steeltoe.Management.Endpoint itself, or some other actuator-registering package) that a test app references NORMALLY (see
    /// <paramref name="repoRootDestination" />'s caller), so it actually participates in restore and shows up in the referencing project's own
    /// project.assets.json - unlike WriteAppProject's own ReferenceOutputAssembly="false" reference to Steeltoe.Management.GitProperties.Build, which was
    /// verified (empirically, against a real "dotnet restore") to be excluded from the resolved dependency graph entirely.
    /// </summary>
    public static string WriteDummyDependencyProject(string repoRootDestination, string projectName)
    {
        string projectDirectory = Path.Combine(repoRootDestination, projectName);
        Directory.CreateDirectory(projectDirectory);

        File.WriteAllText(Path.Combine(projectDirectory, $"{projectName}.csproj"), $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>{TestPaths.TestAppTargetFramework}</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        return projectDirectory;
    }

    /// <summary>
    /// A brand-new, synthetic git repo with a controlled, minimal history (`git init` plus a handful of manufactured commits) - deliberately never a clone
    /// of this (large, real) repository, so the suite stays fast. Returns the repository root (not the TestApp directory - callers combine "TestApp"
    /// themselves, mirroring how multi-project tests place additional sibling projects at the same root).
    /// </summary>
    /// <param name="destination">
    /// The directory to initialize the repository in.
    /// </param>
    /// <param name="commitCount">
    /// The number of manufactured commits to create before the project files are added.
    /// </param>
    /// <param name="gitignoreFallbackFile">
    /// Whether to also list "git.properties" in the repository's .gitignore, modeling the setup a real consumer of $(GitPropertiesWriteToProjectDirectory)
    /// must follow - see SimulateSourcePush for the complementary "not .cfignore'd" half of that same guidance. Defaults to false so most tests (which never
    /// write a fallback file into the project directory at all) aren't given a gitignore entry they don't exercise - only pass true for tests that
    /// specifically cover $(GitPropertiesWriteToProjectDirectory)/the fallback file, so a regression that accidentally wrote one in a test that doesn't
    /// expect it still shows up as an untracked file (and, transitively, as git.dirty=true) instead of being silently absorbed by a blanket ignore rule.
    /// </param>
    public string CreateSyntheticRepo(string destination, int commitCount, bool gitignoreFallbackFile = false)
    {
        Directory.CreateDirectory(destination);
        ProcessRunner.RunGit(destination, "init", "--quiet", "--initial-branch=main", ".");
        ProcessRunner.RunGit(destination, "config", "user.name", "Test User");
        ProcessRunner.RunGit(destination, "config", "user.email", "test@example.com");

        // Without this, dotnet build's own obj/bin output is untracked and git status correctly
        // (but unhelpfully, for these tests) reports the tree as dirty - real projects always
        // gitignore build output, same as this repo does.
        string gitignoreContent = gitignoreFallbackFile
            ? """
            bin/
            obj/
            git.properties
            """
            : """
            bin/
            obj/
            """;

        File.WriteAllText(Path.Combine(destination, ".gitignore"), gitignoreContent);

        for (int commitNumber = 1; commitNumber <= commitCount; commitNumber++)
        {
            File.WriteAllText(Path.Combine(destination, $"file{commitNumber}.txt"), $"content {commitNumber}");
            ProcessRunner.RunGit(destination, "add", "-A");
            ProcessRunner.RunGit(destination, "commit", "--quiet", "-m", $"Commit {commitNumber}");
        }

        CopyCurrentProjectFiles(destination);

        // Commit the project files too, so the synthetic repo starts clean (git.dirty=false)
        // unless a test deliberately makes a further change - otherwise every synthetic repo would
        // show git.dirty=true purely because of these untracked-but-just-added files.
        ProcessRunner.RunGit(destination, "add", "-A");
        ProcessRunner.RunGit(destination, "commit", "--quiet", "-m", "Add project files");
        return destination;
    }

    /// <summary>
    /// Copies a directory tree to a brand-new location with no ".git" anywhere in its ancestry, simulating what actually reaches a running app when deployed
    /// via `cf push` using the dotnet_core_buildpack from source: ".git" is excluded from the pushed tree unconditionally by `cf push` itself (a CLI-level
    /// default, independent of ".cfignore" and not something the buildpack has any special handling for - verified against both tools' source), which is
    /// exactly why live git.properties generation can never run server-side for that scenario. "bin"/"obj" are also excluded, mirroring the ".cfignore"
    /// hygiene real projects need anyway (both to avoid pushing stale local build output, and because reusing another location's "obj" as-is would confuse
    /// MSBuild's own incremental state, which embeds absolute paths). Anything else - including an already-generated fallback "git.properties" sitting next
    /// to the ".csproj" - is copied as-is, exactly as it would ride along in the real push payload.
    /// </summary>
    public static string SimulateSourcePush(string sourceDirectory, string destinationDirectory)
    {
        CopyDirectoryExcluding(new DirectoryInfo(sourceDirectory), destinationDirectory, SimulatedPushExcludedDirectoryNames);
        return destinationDirectory;
    }

    private static void CopyDirectoryExcluding(DirectoryInfo source, string destination, HashSet<string> excludedDirectoryNames)
    {
        Directory.CreateDirectory(destination);

        foreach (FileInfo file in source.GetFiles())
        {
            file.CopyTo(Path.Combine(destination, file.Name), true);
        }

        foreach (DirectoryInfo subDirectory in source.GetDirectories())
        {
            if (excludedDirectoryNames.Contains(subDirectory.Name))
            {
                continue;
            }

            CopyDirectoryExcluding(subDirectory, Path.Combine(destination, subDirectory.Name), excludedDirectoryNames);
        }
    }

    /// <summary>
    /// Copies the shared Steeltoe build infrastructure plus the CURRENT Steeltoe.Management.GitProperties.Build source into the given directory (which
    /// becomes the "repository root" for relative-path purposes, whether or not it's actually a git repository), then writes a TestApp project referencing
    /// it. Returns the TestApp directory.
    /// </summary>
    public string CopyCurrentProjectFiles(string destination)
    {
        CopySharedBuildInfrastructure(destination);
        CopyGitPropertiesBuildSource(destination);
        return WriteAppProject(destination, TestAppProjectName);
    }

    /// <summary>
    /// Packs a FRESH COPY of the current Steeltoe.Management.GitProperties.Build source into a local folder feed, so the PackageReference-based consumption
    /// test exercises the exact same source as every other test here - not a stale .nupkg left behind by an earlier `dotnet build` of the real repo. A plain
    /// filesystem directory is a valid NuGet feed on its own; no server involved. Returns the feed directory: a plain `dotnet build` (not `dotnet pack`)
    /// because GeneratePackageOnBuild already produces the .nupkg as a side effect of a Release build - `dotnet pack` alone does NOT build first here
    /// (IncludeBuildOutput=false makes NuGet's Pack target skip its usual dependency on Build), so it fails with NU5019 ("file not found") against the DLL
    /// our own &lt;None Include="$(TargetPath)"&gt; pack item expects to already exist.
    /// </summary>
    public string PackGitPropertiesBuildToFeed()
    {
        string packSourceDirectory = Path.Combine(RootDirectory, "pack-source");
        CopySharedBuildInfrastructure(packSourceDirectory);
        CopyGitPropertiesBuildSource(packSourceDirectory);

        string projectDirectory = Path.Combine(packSourceDirectory, TestPaths.GitPropertiesBuildRelativePath);
        ProcessResult result = ProcessRunner.RunDotnet(projectDirectory, "build", Path.GetFileName(TestPaths.GitPropertiesBuildProjectFile), "-c", "Release");

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Building/packing {TestPaths.PackageId} failed.\n{result.Output}");
        }

        return Path.Combine(projectDirectory, "bin", "tasks", TestPaths.GitPropertiesBuildTargetFramework);
    }

    /// <summary>
    /// A minimal nuget.config that clears every inherited package source (machine-wide, user-wide, ambient) down to just the given local feed. Without &lt;
    /// clear/&gt;, restore would still see nuget.org and friends - harmless here since nothing else is needed, but clearing makes the test fully
    /// offline-capable and guarantees it's really our local build being consumed, not a same-named/versioned package resolved from somewhere else.
    /// </summary>
    public static void WriteIsolatedNuGetConfig(string filePath, string feedDirectory)
    {
        string content = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="local-git-properties-build" value="{feedDirectory}" />
              </packageSources>
            </configuration>
            """;

        File.WriteAllText(filePath, content);
    }

    /// <summary>
    /// A bare console app that consumes Steeltoe.Management.GitProperties.Build the way a real, external user would - via &lt;PackageReference&gt; against a
    /// built .nupkg - as opposed to every other test here, which uses a ProjectReference/Import straight against source. No explicit &lt;Import&gt; of the
    /// .targets file: that's the whole point of the "build\{PackageId}.targets" NuGet auto-import convention this package relies on, and this is the only
    /// test that actually exercises it end-to-end.
    /// </summary>
    public static void CreatePackageConsumerProject(string projectDirectory, string packageVersion)
    {
        Directory.CreateDirectory(projectDirectory);

        string projectContent = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>{TestPaths.TestAppTargetFramework}</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <!-- This test exists to exercise real PackageReference-based consumption, not the
                     smart default that decides whether generation runs at all - see WriteAppProject's
                     own generateGitProperties parameter for where that IS covered. -->
                <GenerateGitProperties>true</GenerateGitProperties>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Steeltoe.Management.GitProperties.Build" Version="{packageVersion}" />
              </ItemGroup>
            </Project>
            """;

        File.WriteAllText(Path.Combine(projectDirectory, "Consumer.csproj"), projectContent);

        File.WriteAllText(Path.Combine(projectDirectory, "Program.cs"), """
        Console.WriteLine("Hello, World!");
        """);
    }
}
