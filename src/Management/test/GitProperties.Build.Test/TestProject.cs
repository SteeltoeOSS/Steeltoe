// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

/// <summary>
/// A single project directory under test (TestApp, ProjectA, a multi-targeted app, a dummy dependency, a pushed copy, a PackageReference consumer...).
/// Wraps the directory and project name so a test can build/publish it and read whatever git.properties it produced without re-deriving any of those
/// paths itself. Created by <see cref="GitPropertiesTestWorkspace" /> or <see cref="GitRepository" /> - never directly.
/// </summary>
internal sealed class TestProject(string rootDirectory, string name)
{
    private readonly string _debugGitPropertiesFilePath = Path.Combine(rootDirectory, "bin", "Debug", TestPaths.TestAppTargetFramework, "git.properties");

    private readonly string _releasePublishGitPropertiesFilePath =
        Path.Combine(rootDirectory, "bin", "Release", TestPaths.TestAppTargetFramework, "publish", "git.properties");

    public string RootDirectory { get; } = rootDirectory;
    public string Name { get; } = name;

    public string FallbackFilePath { get; } = Path.Combine(rootDirectory, "git.properties");
    public bool GitPropertiesGenerated => File.Exists(_debugGitPropertiesFilePath);
    public bool FallbackGitPropertiesGenerated => File.Exists(FallbackFilePath);
    public bool CompiledAssemblyExists => File.Exists(Path.Combine(RootDirectory, "bin", "Debug", TestPaths.TestAppTargetFramework, $"{Name}.dll"));

    /// <summary>
    /// The relative XML this project's directory/name pair would need to be referenced as a &lt;ProjectReference&gt; from a sibling project - used by the
    /// smart-default detection tests to wire a <see cref="GitRepository.AddDependencyProjectAsync" /> stand-in into the app under test.
    /// </summary>
    public string ToProjectReferenceXml()
    {
        return $"""<ProjectReference Include="..\{Name}\{Name}.csproj" />""";
    }

    public Task<string> BuildAsync(params string[] arguments)
    {
        return RunDotnetAsync("build", arguments);
    }

    public Task<string> PublishAsync(params string[] arguments)
    {
        return RunDotnetAsync("publish", arguments);
    }

    public Task<string> PublishAsync(int exitCodeExpected, params string[] arguments)
    {
        return RunDotnetAsync(exitCodeExpected, "publish", arguments);
    }

    public Task<string> RestoreAsync(params string[] arguments)
    {
        return RunDotnetAsync("restore", arguments);
    }

    private Task<string> RunDotnetAsync(string command, params string[] arguments)
    {
        return ProcessRunner.RunDotnetAsync(RootDirectory, [
            command,
            .. arguments
        ]);
    }

    private Task<string> RunDotnetAsync(int exitCodeExpected, string command, params string[] arguments)
    {
        return ProcessRunner.RunDotnetAsync(RootDirectory, exitCodeExpected, [
            command,
            .. arguments
        ]);
    }

    public Task<Dictionary<string, string>> ReadDebugPropertiesAsync()
    {
        return PropertiesFile.ReadAsync(_debugGitPropertiesFilePath);
    }

    public Task<Dictionary<string, string>> ReadReleasePublishPropertiesAsync()
    {
        return PropertiesFile.ReadAsync(_releasePublishGitPropertiesFilePath);
    }

    public Task<Dictionary<string, string>> ReadFallbackPropertiesAsync()
    {
        return PropertiesFile.ReadAsync(FallbackFilePath);
    }

    /// <summary>
    /// Reads the per-target-framework "bin\Debug\&lt;tfm&gt;\git.properties" produced by a multi-targeted build of this project - see
    /// <see cref="TestPaths.MultiTargetTestFrameworks" />.
    /// </summary>
    public async Task<List<Dictionary<string, string>>> ReadDebugPropertiesPerTargetFrameworkAsync(IEnumerable<string> targetFrameworks)
    {
        List<Dictionary<string, string>> result = [];

        foreach (string targetFramework in targetFrameworks)
        {
            string path = Path.Combine(RootDirectory, "bin", "Debug", targetFramework, "git.properties");
            Dictionary<string, string> properties = await PropertiesFile.ReadAsync(path);
            result.Add(properties);
        }

        return result;
    }
}
