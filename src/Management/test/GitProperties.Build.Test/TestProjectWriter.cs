// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Steeltoe.Management.GitProperties.Build.Test;

internal static partial class TestProjectWriter
{
    private const string HelloWorldCode = """
        Console.WriteLine("Hello, World!");
        """;

    private const string GitPropertiesBuildRelativePath = "src/Management/src/GitProperties.Build";

    private static readonly string[] SharedBuildInfrastructureFiles =
    [
        "shared.props",
        "shared-package.props",
        "shared-project.props",
        "versions.props",
        "stylecop.json",
        "PackageIcon.png",
        "PackageReadme.md",
        "Steeltoe.Debug.ruleset",
        "Steeltoe.Release.ruleset"
    ];

    private static readonly Task<string> RepositoryRootTask = ResolveRepositoryRootAsync();

    private static async Task CopyGitPropertiesBuildSourceAsync(string destinationDirectory)
    {
        string basePath = Path.Combine(destinationDirectory, GitPropertiesBuildRelativePath);
        Directory.CreateDirectory(Path.Combine(basePath, "build"));

        string projectFile = await GetGitPropertiesBuildProjectFileAsync();
        string targetsFile = await GetTargetsFileAsync();
        string markerFile = await GetSourceCheckoutMarkerFileAsync();
        string buildDirectory = await GetGitPropertiesBuildDirectoryAsync();

        File.Copy(projectFile, Path.Combine(basePath, Path.GetFileName(projectFile)), true);
        File.Copy(targetsFile, Path.Combine(basePath, "build", Path.GetFileName(targetsFile)), true);
        File.Copy(markerFile, Path.Combine(basePath, Path.GetFileName(markerFile)), true);

        foreach (string sourceFile in Directory.GetFiles(buildDirectory, "*.cs"))
        {
            File.Copy(sourceFile, Path.Combine(basePath, Path.GetFileName(sourceFile)), true);
        }
    }

    private static async Task CopySharedBuildInfrastructureAsync(string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);
        string repositoryRoot = await RepositoryRootTask;

        foreach (string fileName in SharedBuildInfrastructureFiles)
        {
            File.Copy(Path.Combine(repositoryRoot, fileName), Path.Combine(destinationDirectory, fileName), true);
        }
    }

    private static async Task<string> GetGitPropertiesBuildDirectoryAsync()
    {
        string repositoryRoot = await RepositoryRootTask;
        return Path.Combine(repositoryRoot, "src", "Management", "src", "GitProperties.Build");
    }

    private static async Task<string> GetGitPropertiesBuildProjectFileAsync()
    {
        string directory = await GetGitPropertiesBuildDirectoryAsync();
        return Path.Combine(directory, "Steeltoe.Management.GitProperties.Build.csproj");
    }

    private static async Task<string> GetTargetsFileAsync()
    {
        string directory = await GetGitPropertiesBuildDirectoryAsync();
        return Path.Combine(directory, "build", "Steeltoe.Management.GitProperties.Build.targets");
    }

    private static async Task<string> GetSourceCheckoutMarkerFileAsync()
    {
        string directory = await GetGitPropertiesBuildDirectoryAsync();
        return Path.Combine(directory, "SourceCheckout.txt");
    }

    public static async Task<string> GetPackageIdAsync()
    {
        string projectFile = await GetGitPropertiesBuildProjectFileAsync();
        return Path.GetFileNameWithoutExtension(projectFile);
    }

    private static async Task<string> GetGitPropertiesBuildTargetFrameworkAsync()
    {
        string projectFile = await GetGitPropertiesBuildProjectFileAsync();
        string projectContent = await File.ReadAllTextAsync(projectFile, TestContext.Current.CancellationToken);
        Match match = TargetFrameworkRegex().Match(projectContent);

        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not find <TargetFramework> in {projectFile}.");
        }

        return match.Groups[1].Value;
    }

    private static async Task<string> ResolveRepositoryRootAsync([CallerFilePath] string sourceFilePath = "")
    {
        string sourceDirectory = Path.GetDirectoryName(sourceFilePath) ?? throw new InvalidOperationException("Could not determine the test source directory.");
        string output = await ProcessRunner.RunGitAsync(sourceDirectory, CancellationToken.None, "rev-parse", "--show-toplevel");
        return output.Trim().Replace('/', Path.DirectorySeparatorChar);
    }

    public static async Task<string> WriteAppProjectAsync(string destinationDirectory, string projectName, IEnumerable<string>? targetFrameworks = null,
        bool? generateGitProperties = true, string? extraItemGroupContent = null)
    {
        string appDirectory = Path.Combine(destinationDirectory, projectName);
        Directory.CreateDirectory(appDirectory);

        string targetFrameworkElement = targetFrameworks == null
            ? $"<TargetFramework>{TestAppTargetFramework.Default}</TargetFramework>"
            : $"<TargetFrameworks>{string.Join(';', targetFrameworks)}</TargetFrameworks>";

        string generateGitPropertiesElement = string.Empty;

        if (generateGitProperties != null)
        {
            string generateGitPropertiesValue = generateGitProperties.Value ? "true" : "false";
            generateGitPropertiesElement = $"<GenerateGitProperties>{generateGitPropertiesValue}</GenerateGitProperties>";
        }

        string projectFile = await GetGitPropertiesBuildProjectFileAsync();
        string targetsFile = await GetTargetsFileAsync();

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
                <ProjectReference Include="../{GitPropertiesBuildRelativePath}/{Path.GetFileName(projectFile)}">
                  <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
                </ProjectReference>
                {extraItemGroupContent}
              </ItemGroup>

              <Import Project="$(MSBuildThisFileDirectory)../{GitPropertiesBuildRelativePath}/build/{Path.GetFileName(targetsFile)}" />
            </Project>
            """;

        await File.WriteAllTextAsync(Path.Combine(appDirectory, $"{projectName}.csproj"), projectContent, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(appDirectory, "Program.cs"), HelloWorldCode, TestContext.Current.CancellationToken);

        return appDirectory;
    }

    public static async Task<string> WriteFakeGitExecutableProjectAsync(string destinationDirectory, string projectName, string versionOutput)
    {
        string projectDirectory = Path.Combine(destinationDirectory, projectName);
        Directory.CreateDirectory(projectDirectory);

        string projectContent = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>{TestAppTargetFramework.Default}</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
              </PropertyGroup>
            </Project>
            """;

        await File.WriteAllTextAsync(Path.Combine(projectDirectory, $"{projectName}.csproj"), projectContent, TestContext.Current.CancellationToken);

        string gitVersionCode = $"""Console.WriteLine("{versionOutput}");""";
        await File.WriteAllTextAsync(Path.Combine(projectDirectory, "Program.cs"), gitVersionCode, TestContext.Current.CancellationToken);

        return projectDirectory;
    }

    public static async Task<string> WriteDummyDependencyProjectAsync(string destinationDirectory, string projectName)
    {
        string projectDirectory = Path.Combine(destinationDirectory, projectName);
        Directory.CreateDirectory(projectDirectory);

        await File.WriteAllTextAsync(Path.Combine(projectDirectory, $"{projectName}.csproj"), $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>{TestAppTargetFramework.Default}</TargetFramework>
              </PropertyGroup>
            </Project>
            """, TestContext.Current.CancellationToken);

        return projectDirectory;
    }

    public static async Task<string> CopyCurrentProjectFilesAsync(string destination)
    {
        await CopySharedBuildInfrastructureAsync(destination);
        await CopyGitPropertiesBuildSourceAsync(destination);
        return await WriteAppProjectAsync(destination, GitPropertiesTestWorkspace.TestAppProjectName);
    }

    public static async Task<string> PackGitPropertiesBuildToFeedAsync(string workspaceRootDirectory)
    {
        string packSourceDirectory = Path.Combine(workspaceRootDirectory, "pack-source");
        await CopySharedBuildInfrastructureAsync(packSourceDirectory);
        await CopyGitPropertiesBuildSourceAsync(packSourceDirectory);

        string projectDirectory = Path.Combine(packSourceDirectory, GitPropertiesBuildRelativePath);
        string projectFile = await GetGitPropertiesBuildProjectFileAsync();
        await ProcessRunner.RunDotnetAsync(projectDirectory, "build", Path.GetFileName(projectFile), "-c", "Release");

        string targetFramework = await GetGitPropertiesBuildTargetFrameworkAsync();
        return Path.Combine(projectDirectory, "bin", "tasks", targetFramework);
    }

    public static async Task WriteNuGetConfigAsync(string filePath, string feedDirectory)
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

    public static async Task CreatePackageConsumerProjectAsync(string projectDirectory, string packageVersion)
    {
        Directory.CreateDirectory(projectDirectory);

        string projectContent = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>{TestAppTargetFramework.Default}</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <GenerateGitProperties>true</GenerateGitProperties>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Steeltoe.Management.GitProperties.Build" Version="{packageVersion}" />
              </ItemGroup>
            </Project>
            """;

        await File.WriteAllTextAsync(Path.Combine(projectDirectory, "Consumer.csproj"), projectContent, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(projectDirectory, "Program.cs"), HelloWorldCode, TestContext.Current.CancellationToken);
    }

    [GeneratedRegex("<TargetFramework>(.+?)</TargetFramework>")]
    private static partial Regex TargetFrameworkRegex();
}
