// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

internal static class TestProjectWriter
{
    private const string HelloWorldCode = """
        Console.WriteLine("Hello, World!");
        """;

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

    private static async Task CopySharedBuildInfrastructureAsync(string repoRootDestination)
    {
        Directory.CreateDirectory(repoRootDestination);
        string repositoryRoot = await TestPaths.GetRepositoryRootAsync();

        foreach (string fileName in TestPaths.SharedBuildInfrastructureFiles)
        {
            File.Copy(Path.Combine(repositoryRoot, fileName), Path.Combine(repoRootDestination, fileName), true);
        }
    }

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
        await File.WriteAllTextAsync(Path.Combine(appDirectory, "Program.cs"), HelloWorldCode, TestContext.Current.CancellationToken);

        return appDirectory;
    }

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

        string projectDirectory = Path.Combine(packSourceDirectory, TestPaths.GitPropertiesBuildRelativePath);
        string projectFile = await TestPaths.GetGitPropertiesBuildProjectFileAsync();
        await ProcessRunner.RunDotnetAsync(projectDirectory, "build", Path.GetFileName(projectFile), "-c", "Release");

        string targetFramework = await TestPaths.GetGitPropertiesBuildTargetFrameworkAsync();
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
                <TargetFramework>{TestPaths.TestAppTargetFramework}</TargetFramework>
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
}
