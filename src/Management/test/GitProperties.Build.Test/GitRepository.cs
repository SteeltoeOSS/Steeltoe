// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

internal sealed class GitRepository(TestWorkspace workspace, string rootDirectory, TestProject testApp)
{
    private readonly string _sharedCacheFilePath = Path.Combine(rootDirectory, "obj", "git.properties.cache");
    private string RootDirectory { get; } = rootDirectory;

    public TestProject TestApp { get; } = testApp;
    public bool SharedCacheExists => File.Exists(_sharedCacheFilePath);

    public Task<string> RunGitAsync(params string[] arguments)
    {
        return ProcessRunner.RunGitAsync(RootDirectory, arguments);
    }

    public Task<string> GetCommitIdAsync()
    {
        return RunGitAsync("rev-parse", "HEAD");
    }

    public async Task<bool> IsDirtyAsync()
    {
        string status = await RunGitAsync("status", "--porcelain");
        return status.Length > 0;
    }

    public Task TagAsync(string name, string? commitId = null)
    {
        return commitId == null ? RunGitAsync("tag", name) : RunGitAsync("tag", name, commitId);
    }

    public async Task<GitRepository> CloneAsShallowAsync(string name, int depth = 1)
    {
        string destination = workspace.GetPath(name);
        // --no-local is required: for a local path, git's local-clone optimization otherwise bypasses shallow-transfer logic entirely and silently ignores --depth, producing a full clone.
        await ProcessRunner.RunGitAsync(Path.GetTempPath(), "clone", "--quiet", "--no-local", "--depth", $"{depth}", RootDirectory, destination);

        TestProject shallowTestApp = GetExistingTestApp(destination);
        return new GitRepository(workspace, destination, shallowTestApp);
    }

    public async Task<GitRepository> AddWorktreeAsync(string name, string newBranchName)
    {
        string destination = workspace.GetPath(name);
        await RunGitAsync("worktree", "add", "--quiet", "-b", newBranchName, destination);

        TestProject worktreeTestApp = GetExistingTestApp(destination);
        return new GitRepository(workspace, destination, worktreeTestApp);
    }

    public async Task<GitRepository> AddSubmoduleAsync(string name, GitRepository sourceRepository)
    {
        // protocol.file.allow is required: git blocks local-path submodule sources by default.
        await RunGitAsync("-c", "protocol.file.allow=always", "submodule", "add", "--quiet", sourceRepository.RootDirectory, name);
        await GitRepositoryBuilder.CommitAllAsync(RootDirectory, $"Add submodule {name}");

        string destination = Path.Combine(RootDirectory, name);
        TestProject submoduleTestApp = GetExistingTestApp(destination);
        return new GitRepository(workspace, destination, submoduleTestApp);
    }

    public void DeleteSharedCache()
    {
        File.Delete(_sharedCacheFilePath);
    }

    public RemotePushProjectTree SimulatePush(string name)
    {
        string destination = workspace.GetPath(name);
        GitRepositoryBuilder.SimulateSourcePush(RootDirectory, destination);

        TestProject pushedTestApp = GetExistingTestApp(destination);
        return new RemotePushProjectTree(destination, pushedTestApp);
    }

    private static TestProject GetExistingTestApp(string repositoryDirectory)
    {
        string appDirectory = Path.Combine(repositoryDirectory, TestWorkspace.TestAppProjectName);
        return new TestProject(appDirectory, TestWorkspace.TestAppProjectName);
    }

    public async Task<TestProject> AddTestAppAsync(string name, PackageReference[]? packageReferences = null, TestProject[]? projectReferences = null,
        string[]? targetFrameworks = null)
    {
        ProjectFileBuilder builder = BuildProjectFile(packageReferences, projectReferences);

        if (targetFrameworks != null)
        {
            builder.WithTargetFrameworks(targetFrameworks);
        }

        string projectDirectory = await TestProjectWriter.WriteAppProjectAsync(RootDirectory, name, builder);
        return new TestProject(projectDirectory, name);
    }

    public async Task<TestProject> AddTestLibraryAsync(string name, bool? generateGitProperties = null, PackageReference[]? packageReferences = null,
        TestProject[]? projectReferences = null)
    {
        ProjectFileBuilder builder = BuildProjectFile(packageReferences, projectReferences);

        if (generateGitProperties != null)
        {
            builder.WithGenerateGitProperties(generateGitProperties.Value);
        }

        string projectDirectory = await TestProjectWriter.WriteLibraryProjectAsync(RootDirectory, name, builder);
        return new TestProject(projectDirectory, name);
    }

    private static ProjectFileBuilder BuildProjectFile(PackageReference[]? packageReferences, TestProject[]? projectReferences)
    {
        var builder = new ProjectFileBuilder();

        foreach (PackageReference packageReference in packageReferences ?? [])
        {
            builder.WithPackageReference(packageReference);
        }

        foreach (TestProject testProject in projectReferences ?? [])
        {
            builder.WithProjectReference(testProject.ToProjectReference());
        }

        return builder;
    }
}
