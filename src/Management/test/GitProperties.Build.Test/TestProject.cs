// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

internal sealed class TestProject(string rootDirectory, string name)
{
    private readonly string _debugGitPropertiesFilePath = Path.Combine(rootDirectory, "bin", "Debug", TestAppTargetFramework.Default, "git.properties");

    private readonly string _releasePublishGitPropertiesFilePath =
        Path.Combine(rootDirectory, "bin", "Release", TestAppTargetFramework.Default, "publish", "git.properties");

    private bool _hasRestored;

    public string RootDirectory { get; } = rootDirectory;
    public string Name { get; } = name;

    public string FallbackFilePath { get; } = Path.Combine(rootDirectory, "git.properties");
    public bool GitPropertiesGenerated => File.Exists(_debugGitPropertiesFilePath);
    public bool FallbackGitPropertiesGenerated => File.Exists(FallbackFilePath);
    public bool CompiledAssemblyExists => File.Exists(Path.Combine(RootDirectory, "bin", "Debug", TestAppTargetFramework.Default, $"{Name}.dll"));

    public string ToProjectReferenceXml()
    {
        return $"""<ProjectReference Include="..\{Name}\{Name}.csproj" />""";
    }

    public async Task<DotNetCommandOutput> BuildAsync(params string[] arguments)
    {
        return await BuildAsync(0, null, arguments);
    }

    public async Task<DotNetCommandOutput> BuildAsync(int exitCodeExpected, Dictionary<string, string>? environmentVariables, params string[] arguments)
    {
        return await RunDotNetCommandAsync("build", exitCodeExpected, environmentVariables, arguments);
    }

    public async Task<DotNetCommandOutput> PublishAsync(params string[] arguments)
    {
        return await PublishAsync(0, arguments);
    }

    public async Task<DotNetCommandOutput> PublishAsync(int exitCodeExpected, params string[] arguments)
    {
        return await RunDotNetCommandAsync("publish", exitCodeExpected, null, arguments);
    }

    public async Task<DotNetCommandOutput> RestoreAsync(params string[] arguments)
    {
        return await RunDotNetCommandAsync("restore", 0, null, arguments);
    }

    private async Task<DotNetCommandOutput> RunDotNetCommandAsync(string command, int exitCodeExpected, Dictionary<string, string>? environmentVariables,
        params string[] arguments)
    {
        // Avoid redundant restore of repeated build/publish calls in the same test to improve performance.
        bool skipRestore = _hasRestored && command != "restore";

        string output = await ProcessRunner.RunDotNetAsync(RootDirectory, exitCodeExpected, environmentVariables, [
            command,
            .. skipRestore ? ["--no-restore"] : Array.Empty<string>(),
            .. arguments
        ]);

        _hasRestored = true;
        return new DotNetCommandOutput(output);
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
