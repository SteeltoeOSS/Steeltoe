// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace Steeltoe.Management.GitProperties.Build.Test;

internal sealed class GitPropertiesTestWorkspace : IDisposable
{
    public const string TestAppProjectName = "TestApp";

    private static readonly Task<string> NonZeroExitCodeGitExecutableTask = GetOrCreateNonZeroExitCodeGitExecutableAsync();
    private readonly string _rootDirectory;

    private GitPropertiesTestWorkspace(string rootDirectory)
    {
        _rootDirectory = rootDirectory;
    }

    public static async Task<GitPropertiesTestWorkspace> CreateAsync()
    {
        string rootDirectory = Path.Combine(Path.GetTempPath(), $"build-tasks-test_{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)[..8]}");
        Directory.CreateDirectory(rootDirectory);
        string physicalRootDirectory = await ResolvePhysicalPathAsync(rootDirectory);
        return new GitPropertiesTestWorkspace(physicalRootDirectory);
    }

    private static async Task<string> ResolvePhysicalPathAsync(string path)
    {
        if (OperatingSystem.IsMacOS())
        {
            // On macOS, $TMPDIR resolves through a symlink (/var -> /private/var).
            string output = await ProcessRunner.RunPwdAsync(path);
            return output.Trim();
        }

        return path;
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

    public string GetPath(string name)
    {
        return Path.Combine(_rootDirectory, name);
    }

    public async Task<TestProject> CreateProjectWithoutGitAsync(string name)
    {
        string directory = GetPath(name);
        Directory.CreateDirectory(directory);
        string appDirectory = await TestProjectWriter.WriteDefaultTestAppProjectAsync(directory);
        return new TestProject(appDirectory, TestAppProjectName);
    }

    public async Task<string> CreateFakeGitExecutableAsync(string versionOutput)
    {
        string projectDirectory = await TestProjectWriter.WriteFakeGitExecutableProjectAsync(_rootDirectory, "FakeGit", versionOutput);
        await ProcessRunner.RunDotNetAsync(projectDirectory, 0, null, "build");

        string executableName = OperatingSystem.IsWindows() ? "FakeGit.exe" : "FakeGit";
        return Path.Combine(projectDirectory, "bin", "Debug", TestAppTargetFramework.Default, executableName);
    }

    public static Task<string> GetNonZeroExitCodeGitExecutableAsync()
    {
        return NonZeroExitCodeGitExecutableTask;
    }

    private static async Task<string> GetOrCreateNonZeroExitCodeGitExecutableAsync()
    {
        string projectDirectory = Path.Combine(Path.GetTempPath(), "steeltoe-nonzero-exit-git", "NonZeroExitCodeGit");
        string executableName = OperatingSystem.IsWindows() ? "NonZeroExitCodeGit.exe" : "NonZeroExitCodeGit";
        string executablePath = Path.Combine(projectDirectory, "bin", "Debug", TestAppTargetFramework.Default, executableName);

        if (!File.Exists(executablePath))
        {
            await TestProjectWriter.WriteNonZeroExitCodeGitExecutableProjectAsync(projectDirectory, "NonZeroExitCodeGit");
            await ProcessRunner.RunDotNetAsync(projectDirectory, 0, null, "build");
        }

        return executablePath;
    }

    public async Task<EmptyGitRepository> CreateEmptyRepositoryAsync(string name)
    {
        string directory = GetPath(name);
        await GitRepositoryBuilder.InitializeEmptyAsync(directory);
        return new EmptyGitRepository(this, directory);
    }

    public async Task<GitRepository> CreateGitRepositoryAsync(string name, int commitCount, bool includeFallbackFileInGitignore = false)
    {
        string directory = GetPath(name);
        await GitRepositoryBuilder.InitializeAsync(directory, commitCount, includeFallbackFileInGitignore);
        TestProject testApp = await GitRepository.WriteDefaultTestAppAsync(directory);
        var repository = new GitRepository(this, directory, testApp);
        await GitRepositoryBuilder.CommitAllAsync(directory, "Add project files");
        return repository;
    }

    public Task<string> PackGitPropertiesBuildToFeedAsync()
    {
        return TestProjectWriter.PackGitPropertiesBuildToFeedAsync(_rootDirectory);
    }

    public Task<string> GetPackageIdAsync()
    {
        return TestProjectWriter.GetPackageIdAsync();
    }

    public Task WriteIsolatedNuGetConfigAsync(TestProject project, string feedDirectory)
    {
        return TestProjectWriter.WriteNuGetConfigAsync(Path.Combine(project.RootDirectory, "nuget.config"), feedDirectory);
    }

    public async Task WriteFileAsync(string path, string contents)
    {
        await File.WriteAllTextAsync(path, contents, TestContext.Current.CancellationToken);
    }

    public async Task WriteFileAsync(string path, IEnumerable<string> lines)
    {
        await File.WriteAllLinesAsync(path, lines, TestContext.Current.CancellationToken);
    }

    public void Dispose()
    {
        try
        {
            // git marks files under .git\objects read-only on Windows, which makes a plain recursive delete throw UnauthorizedAccessException.
            ClearReadOnlyAttributes(new DirectoryInfo(_rootDirectory));
            Directory.Delete(_rootDirectory, true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Best-effort cleanup only: a transiently locked file (e.g. an antivirus scan) must not fail the test run.
        }
    }
}
