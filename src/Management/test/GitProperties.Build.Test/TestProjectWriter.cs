// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

/// <summary>
/// Writes the synthetic project files (.csproj/Program.cs/nuget.config) that dev-loop and PackageReference-consumer tests build against, and copies the
/// CURRENT Steeltoe.Management.GitProperties.Build source - plus the handful of root-level Steeltoe build infrastructure files its own "shared.props"
/// import chain needs - into a synthetic test repository, so every test exercises whatever is on disk right now, never a stale git-tracked copy.
/// </summary>
internal static class TestProjectWriter
{
    /// <summary>
    /// Copies the CURRENT Steeltoe.Management.GitProperties.Build source (csproj + all .cs files + the targets file + SourceCheckout.txt) into
    /// "src\Management\src\GitProperties.Build" under the given repository root - the exact relative depth its own csproj's "shared.props" import expects.
    /// Copying the marker file matters: without it, $(GitPropertiesTaskHost) in Steeltoe.Management.GitProperties.Build.targets would (correctly, but
    /// unhelpfully for these tests) detect "packaged" consumption instead of the dev loop this is meant to simulate, silently skipping the TaskHostFactory
    /// path every other test here relies on.
    /// </summary>
    private static async Task CopyGitPropertiesBuildSourceAsync(string repoRootDestination)
    {
        string destination = Path.Combine(repoRootDestination, TestPaths.GitPropertiesBuildRelativePath);
        Directory.CreateDirectory(Path.Combine(destination, "build"));

        string projectFile = await TestPaths.GetGitPropertiesBuildProjectFileAsync();
        string targetsFile = await TestPaths.GetTargetsFileAsync();
        string markerFile = await TestPaths.GetSourceCheckoutMarkerFileAsync();
        string buildDirectory = await TestPaths.GetGitPropertiesBuildDirectoryAsync();

        File.Copy(projectFile, Path.Combine(destination, Path.GetFileName(projectFile)), true);
        File.Copy(targetsFile, Path.Combine(destination, "build", Path.GetFileName(targetsFile)), true);
        File.Copy(markerFile, Path.Combine(destination, Path.GetFileName(markerFile)), true);

        foreach (string sourceFile in Directory.GetFiles(buildDirectory, "*.cs"))
        {
            File.Copy(sourceFile, Path.Combine(destination, Path.GetFileName(sourceFile)), true);
        }
    }

    /// <summary>
    /// Copies the handful of root-level Steeltoe build infrastructure files that Steeltoe.Management.GitProperties.Build.csproj's own shared.props import
    /// chain needs (versioning, packaging defaults, analyzers), so a synthetic test repo - which has none of Steeltoe's other real projects - still
    /// evaluates the same properties a real build inside the repository would.
    /// </summary>
    private static async Task CopySharedBuildInfrastructureAsync(string repoRootDestination)
    {
        Directory.CreateDirectory(repoRootDestination);
        string repositoryRoot = await TestPaths.GetRepositoryRootAsync();

        foreach (string fileName in TestPaths.SharedBuildInfrastructureFiles)
        {
            File.Copy(Path.Combine(repositoryRoot, fileName), Path.Combine(repoRootDestination, fileName), true);
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
    /// The directory to write the project under - typically a repository root returned by <see cref="GitPropertiesTestWorkspace.CreateGitRepositoryAsync" />
    /// .
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
    /// DetectConsumingPackageReferenceTask/$(GitPropertiesConsumingPackageIds) - see <see cref="WriteDummyDependencyProjectAsync" />.
    /// </param>
    /// <param name="extraItemGroupContent">
    /// Extra raw XML inserted into the same &lt;ItemGroup&gt; as the ProjectReference above - currently only used to add a second, normal (not
    /// ReferenceOutputAssembly="false") ProjectReference to a <see cref="WriteDummyDependencyProjectAsync" /> stand-in, so it actually participates in
    /// restore and shows up in this project's own project.assets.json (see DetectConsumingPackageReferenceTask's remarks for why that distinction matters).
    /// </param>
    public static async Task<string> WriteAppProjectAsync(string repoRootDestination, string projectName, string? targetFrameworks = null,
        bool? generateGitProperties = true, string? extraItemGroupContent = null)
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

        string projectFile = await TestPaths.GetGitPropertiesBuildProjectFileAsync();
        string targetsFile = await TestPaths.GetTargetsFileAsync();

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
                <ProjectReference Include="../{TestPaths.GitPropertiesBuildRelativePath}/{Path.GetFileName(projectFile)}">
                  <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
                </ProjectReference>
                {extraItemGroupContent}
              </ItemGroup>

              <Import Project="$(MSBuildThisFileDirectory)../{TestPaths.GitPropertiesBuildRelativePath}/build/{Path.GetFileName(targetsFile)}" />
            </Project>
            """;

        await File.WriteAllTextAsync(Path.Combine(appDirectory, $"{projectName}.csproj"), projectContent, TestContext.Current.CancellationToken);

        await File.WriteAllTextAsync(Path.Combine(appDirectory, "Program.cs"), """
        Console.WriteLine("Hello, World!");
        """, TestContext.Current.CancellationToken);

        return appDirectory;
    }

    /// <summary>
    /// Writes a minimal, do-nothing class library project - used only by the smart-default detection tests as a stand-in for a real dependency (e.g.
    /// Steeltoe.Management.Endpoint itself, or some other actuator-registering package) that a test app references NORMALLY (see
    /// <paramref name="repoRootDestination" />'s caller), so it actually participates in restore and shows up in the referencing project's own
    /// project.assets.json - unlike WriteAppProjectAsync's own ReferenceOutputAssembly="false" reference to Steeltoe.Management.GitProperties.Build, which
    /// was verified (empirically, against a real "dotnet restore") to be excluded from the resolved dependency graph entirely.
    /// </summary>
    public static async Task<string> WriteDummyDependencyProjectAsync(string repoRootDestination, string projectName)
    {
        string projectDirectory = Path.Combine(repoRootDestination, projectName);
        Directory.CreateDirectory(projectDirectory);

        await File.WriteAllTextAsync(Path.Combine(projectDirectory, $"{projectName}.csproj"), $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>{TestPaths.TestAppTargetFramework}</TargetFramework>
              </PropertyGroup>
            </Project>
            """, TestContext.Current.CancellationToken);

        return projectDirectory;
    }

    /// <summary>
    /// Copies the shared Steeltoe build infrastructure plus the CURRENT Steeltoe.Management.GitProperties.Build source into the given directory (which
    /// becomes the "repository root" for relative-path purposes, whether or not it's actually a git repository), then writes a TestApp project referencing
    /// it. Returns the TestApp directory.
    /// </summary>
    public static async Task<string> CopyCurrentProjectFilesAsync(string destination)
    {
        await CopySharedBuildInfrastructureAsync(destination);
        await CopyGitPropertiesBuildSourceAsync(destination);
        return await WriteAppProjectAsync(destination, GitPropertiesTestWorkspace.TestAppProjectName);
    }

    /// <summary>
    /// Packs a FRESH COPY of the current Steeltoe.Management.GitProperties.Build source into a local folder feed, so the PackageReference-based consumption
    /// test exercises the exact same source as every other test here - not a stale .nupkg left behind by an earlier `dotnet build` of the real repo. A plain
    /// filesystem directory is a valid NuGet feed on its own; no server involved. Returns the feed directory: a plain `dotnet build` (not `dotnet pack`)
    /// because GeneratePackageOnBuild already produces the .nupkg as a side effect of a Release build - `dotnet pack` alone does NOT build first here
    /// (IncludeBuildOutput=false makes NuGet's Pack target skip its usual dependency on Build), so it fails with NU5019 ("file not found") against the DLL
    /// our own &lt;None Include="$(TargetPath)"&gt; pack item expects to already exist.
    /// </summary>
    /// <param name="workspaceRootDirectory">
    /// The calling <see cref="GitPropertiesTestWorkspace" />'s own <see cref="GitPropertiesTestWorkspace.RootDirectory" /> - the pack source is written
    /// under a "pack-source" subdirectory of it, kept separate from anything else the workspace writes.
    /// </param>
    public static async Task<string> PackGitPropertiesBuildToFeedAsync(string workspaceRootDirectory)
    {
        string packSourceDirectory = Path.Combine(workspaceRootDirectory, "pack-source");
        await CopySharedBuildInfrastructureAsync(packSourceDirectory);
        await CopyGitPropertiesBuildSourceAsync(packSourceDirectory);

        string projectDirectory = Path.Combine(packSourceDirectory, TestPaths.GitPropertiesBuildRelativePath);
        string projectFile = await TestPaths.GetGitPropertiesBuildProjectFileAsync();
        await ProcessRunner.RunDotnetAsync(projectDirectory, "build", Path.GetFileName(projectFile), "-c", "Release");

        string targetFramework = await TestPaths.GetGitPropertiesBuildTargetFrameworkAsync();
        return Path.Combine(projectDirectory, "bin", "tasks", targetFramework);
    }

    /// <summary>
    /// A minimal nuget.config that clears every inherited package source (machine-wide, user-wide, ambient) down to just the given local feed. Without &lt;
    /// clear/&gt;, restore would still see nuget.org and friends - harmless here since nothing else is needed, but clearing makes the test fully
    /// offline-capable and guarantees it's really our local build being consumed, not a same-named/versioned package resolved from somewhere else.
    /// </summary>
    public static async Task WriteIsolatedNuGetConfigAsync(string filePath, string feedDirectory)
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

        await File.WriteAllTextAsync(filePath, content, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// A bare console app that consumes Steeltoe.Management.GitProperties.Build the way a real, external user would - via &lt;PackageReference&gt; against a
    /// built .nupkg - as opposed to every other test here, which uses a ProjectReference/Import straight against source. No explicit &lt;Import&gt; of the
    /// .targets file: that's the whole point of the "build\{PackageId}.targets" NuGet auto-import convention this package relies on, and this is the only
    /// test that actually exercises it end-to-end.
    /// </summary>
    public static async Task CreatePackageConsumerProjectAsync(string projectDirectory, string packageVersion)
    {
        Directory.CreateDirectory(projectDirectory);

        string projectContent = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>{TestPaths.TestAppTargetFramework}</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <!-- This test exists to exercise real PackageReference-based consumption, not the
                     smart default that decides whether generation runs at all - see WriteAppProjectAsync's
                     own generateGitProperties parameter for where that IS covered. -->
                <GenerateGitProperties>true</GenerateGitProperties>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Steeltoe.Management.GitProperties.Build" Version="{packageVersion}" />
              </ItemGroup>
            </Project>
            """;

        await File.WriteAllTextAsync(Path.Combine(projectDirectory, "Consumer.csproj"), projectContent, TestContext.Current.CancellationToken);

        await File.WriteAllTextAsync(Path.Combine(projectDirectory, "Program.cs"), """
        Console.WriteLine("Hello, World!");
        """, TestContext.Current.CancellationToken);
    }
}
