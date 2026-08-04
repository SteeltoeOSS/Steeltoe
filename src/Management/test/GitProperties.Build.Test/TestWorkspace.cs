// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

internal sealed class TestWorkspace : IDisposable
{
    public const string TestAppProjectName = "TestApp";

    private static readonly Task<string> NonZeroExitCodeGitExecutableTask = GetOrCreateNonZeroExitCodeGitExecutableAsync();
    private readonly string _rootDirectory;

    public PackageReference GitPropertiesPackageReference { get; } = PackGitPropertiesSourceOnceFixture.GitPropertiesPackageReference;
    public PackageReference FakeEndpointPackageReference { get; } = PackGitPropertiesSourceOnceFixture.FakeEndpointPackageReference;

    private TestWorkspace(string rootDirectory)
    {
        _rootDirectory = rootDirectory;
    }

    public PackageReference GetGitPropertiesPackageReferenceWithPrivateAssets(string? privateAssets)
    {
        return GitPropertiesPackageReference with
        {
            PrivateAssets = privateAssets
        };
    }

    public static TestWorkspace Create()
    {
        string testDirectory = Path.Combine(PackGitPropertiesSourceOnceFixture.SessionDirectory, "tests", TestContext.Current.TestClass!.TestClassSimpleName);
        Directory.CreateDirectory(testDirectory);
        return new TestWorkspace(testDirectory);
    }

    public string GetPath(string name)
    {
        return Path.Combine(_rootDirectory, name);
    }

    public async Task<TestProject> CreateProjectWithoutGitAsync(string name)
    {
        string directory = GetPath(name);
        Directory.CreateDirectory(directory);

        await WriteNuGetConfigAsync(directory);
        return await WriteDefaultTestAppAsync(directory);
    }

    public async Task<TestProject> WriteDefaultTestAppAsync(string destinationDirectory)
    {
        ProjectFileBuilder builder = new ProjectFileBuilder().WithPackageReference(GitPropertiesPackageReference)
            .WithPackageReference(FakeEndpointPackageReference);

        string appDirectory = await TestProjectWriter.WriteAppProjectAsync(destinationDirectory, TestAppProjectName, builder);
        return new TestProject(appDirectory, TestAppProjectName);
    }

    public async Task<string> CreateFakeGitExecutableAsync(string versionOutput)
    {
        string projectDirectory = await TestProjectWriter.WriteFakeGitProjectAsync(_rootDirectory, "FakeGit", versionOutput);
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
        string parentDirectory = Path.Combine(Path.GetTempPath(), "steeltoe-nonzero-exit-git");
        string projectDirectory = Path.Combine(parentDirectory, "NonZeroExitCodeGit");
        string executableName = OperatingSystem.IsWindows() ? "NonZeroExitCodeGit.exe" : "NonZeroExitCodeGit";
        string executablePath = Path.Combine(projectDirectory, "bin", "Debug", TestAppTargetFramework.Default, executableName);

        if (!File.Exists(executablePath))
        {
            await TestProjectWriter.WriteNonZeroExitCodeGitProjectAsync(parentDirectory, "NonZeroExitCodeGit");
            await ProcessRunner.RunDotNetAsync(projectDirectory, 0, null, "build");
        }

        return executablePath;
    }

    public async Task<EmptyGitRepository> CreateEmptyRepositoryAsync(string name)
    {
        string directory = GetPath(name);
        await GitRepositoryBuilder.InitializeEmptyAsync(directory);
        await WriteNuGetConfigAsync(directory);
        return new EmptyGitRepository(this, directory);
    }

    public async Task<GitRepository> CreateGitRepositoryAsync(string name, int commitCount, bool includeFallbackFileInGitignore = false)
    {
        string directory = GetPath(name);
        await GitRepositoryBuilder.InitializeAsync(directory, commitCount, includeFallbackFileInGitignore);
        await WriteNuGetConfigAsync(directory);

        TestProject testApp = await WriteDefaultTestAppAsync(directory);
        var repository = new GitRepository(this, directory, testApp);
        await GitRepositoryBuilder.CommitAllAsync(directory, "Add project files");
        return repository;
    }

    private async Task WriteNuGetConfigAsync(string directory)
    {
        await TestProjectWriter.WriteNuGetConfigAsync(directory, PackGitPropertiesSourceOnceFixture.Source);
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
            // Git marks files under .git\objects read-only on Windows, which makes a plain recursive delete throw UnauthorizedAccessException.
            ClearReadOnlyAttributesUnderGitDirectories(new DirectoryInfo(_rootDirectory));
            Directory.Delete(_rootDirectory, true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Best-effort cleanup only: a transiently locked file (e.g. an antivirus scan) must not fail the test run.
        }
    }

    private static void ClearReadOnlyAttributesUnderGitDirectories(DirectoryInfo directory)
    {
        foreach (DirectoryInfo subdirectory in directory.GetDirectories())
        {
            if (subdirectory.Name == ".git")
            {
                ClearReadOnlyAttributes(subdirectory);
            }
            else
            {
                ClearReadOnlyAttributesUnderGitDirectories(subdirectory);
            }
        }
    }

    private static void ClearReadOnlyAttributes(DirectoryInfo directory)
    {
        foreach (FileInfo file in directory.GetFiles())
        {
            file.Attributes = FileAttributes.Normal;
        }

        foreach (DirectoryInfo subdirectory in directory.GetDirectories())
        {
            ClearReadOnlyAttributes(subdirectory);
        }
    }
}
