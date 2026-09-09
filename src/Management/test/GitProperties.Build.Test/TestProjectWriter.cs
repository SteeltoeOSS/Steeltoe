// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

internal static class TestProjectWriter
{
    private const string HelloWorldSource = """
        Console.WriteLine("Hello, World!");
        """;

    private const string NonZeroExitSource = """
        using System.Diagnostics.CodeAnalysis;

        [assembly: ExcludeFromCodeCoverage]

        return 1;
        """;

    public static Task<string> WriteAppProjectAsync(string destinationDirectory, string projectName, ProjectFileBuilder builder)
    {
        return WriteProjectAsync(destinationDirectory, projectName, builder, HelloWorldSource);
    }

    public static Task<string> WriteLibraryProjectAsync(string destinationDirectory, string projectName, ProjectFileBuilder builder)
    {
        return WriteProjectAsync(destinationDirectory, projectName, builder);
    }

    public static Task<string> WriteFakeGitProjectAsync(string destinationDirectory, string projectName, string versionOutput)
    {
        string printVersionSource = $"""
            using System.Diagnostics.CodeAnalysis;

            [assembly: ExcludeFromCodeCoverage]

            Console.WriteLine("{versionOutput}");
            """;

        var builder = new ProjectFileBuilder();
        return WriteProjectAsync(destinationDirectory, projectName, builder, printVersionSource);
    }

    public static Task WriteNonZeroExitCodeGitProjectAsync(string destinationDirectory, string projectName)
    {
        var builder = new ProjectFileBuilder();
        return WriteProjectAsync(destinationDirectory, projectName, builder, NonZeroExitSource);
    }

    private static async Task<string> WriteProjectAsync(string destinationDirectory, string projectName, ProjectFileBuilder projectFileBuilder,
        string? programContent = null)
    {
        string projectDirectory = new DirectoryInfo(destinationDirectory).CreateSubdirectory(projectName).FullName;

        if (programContent != null)
        {
            string programFilePath = Path.Combine(projectDirectory, "Program.cs");
            await File.WriteAllTextAsync(programFilePath, programContent, TestContext.Current.CancellationToken);

            projectFileBuilder.IsExecutable = true;
        }

        string projectContent = projectFileBuilder.Build();
        string projectFilePath = Path.Combine(projectDirectory, $"{projectName}.csproj");
        await File.WriteAllTextAsync(projectFilePath, projectContent, TestContext.Current.CancellationToken);

        return projectDirectory;
    }

    public static async Task WriteNuGetConfigAsync(string destinationDirectory, NuGetSource source)
    {
        string content = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <config>
                <add key="globalPackagesFolder" value="{source.PackagesDirectory}" />
              </config>
              <packageSources>
                <clear />
                <add key="local-git-properties" value="{source.FeedDirectory}" />
              </packageSources>
            </configuration>
            """;

        string filePath = Path.Combine(destinationDirectory, "nuget.config");
        await File.WriteAllTextAsync(filePath, content, TestContext.Current.CancellationToken);
    }
}
