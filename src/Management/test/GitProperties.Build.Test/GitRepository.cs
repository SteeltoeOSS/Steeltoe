// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

internal sealed class GitRepository(GitPropertiesTestWorkspace workspace, string rootDirectory, TestProject testApp)
{
    private const string ConsumerAppProjectName = "ConsumerApp";

    private readonly string _rootDirectory = rootDirectory;
    private readonly string _sharedCacheFilePath = Path.Combine(rootDirectory, "obj", "git.properties.cache");

    public TestProject TestApp { get; } = testApp;
    public bool SharedCacheExists => File.Exists(_sharedCacheFilePath);

    public Task<string> RunGitAsync(params string[] arguments)
    {
        return ProcessRunner.RunGitAsync(_rootDirectory, arguments);
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

    public async Task<TestProject> AddProjectAsync(string name, IEnumerable<string>? targetFrameworks = null, bool? generateGitProperties = true,
        string? extraItemGroupContent = null)
    {
        string projectDirectory = await TestProjectWriter.WriteAppProjectAsync(_rootDirectory, name, targetFrameworks, generateGitProperties,
            extraItemGroupContent);

        return new TestProject(projectDirectory, name);
    }

    public async Task<TestProject> AddDependencyProjectAsync(string name)
    {
        string projectDirectory = await TestProjectWriter.WriteDummyDependencyProjectAsync(_rootDirectory, name);
        return new TestProject(projectDirectory, name);
    }

    public Task<TestProject> AddTestAppReferencingAsync(TestProject dependency)
    {
        string extraItemGroupContent = dependency.ToProjectReferenceXml();
        return AddProjectAsync(ConsumerAppProjectName, generateGitProperties: null, extraItemGroupContent: extraItemGroupContent);
    }

    public async Task<TestProject> AddPackageConsumerProjectAsync(string name, string packageVersion)
    {
        string projectDirectory = Path.Combine(_rootDirectory, name);
        await TestProjectWriter.CreatePackageConsumerProjectAsync(projectDirectory, packageVersion);
        return new TestProject(projectDirectory, name);
    }

    public async Task<GitRepository> CloneAsShallowAsync(string name, int depth = 1)
    {
        string destination = workspace.GetPath(name);
        // --no-local is required: for a local path, git's local-clone optimization otherwise bypasses shallow-transfer logic entirely and silently ignores --depth, producing a full clone.
        await ProcessRunner.RunGitAsync(Path.GetTempPath(), "clone", "--quiet", "--no-local", "--depth", $"{depth}", _rootDirectory, destination);

        TestProject shallowTestApp = await WriteDefaultTestAppAsync(destination);
        return new GitRepository(workspace, destination, shallowTestApp);
    }

    public async Task<GitRepository> AddWorktreeAsync(string name, string newBranchName)
    {
        string destination = workspace.GetPath(name);
        await RunGitAsync("worktree", "add", "--quiet", "-b", newBranchName, destination);

        TestProject worktreeTestApp = await WriteDefaultTestAppAsync(destination);
        return new GitRepository(workspace, destination, worktreeTestApp);
    }

    public async Task<GitRepository> AddSubmoduleAsync(string name, GitRepository sourceRepository)
    {
        // protocol.file.allow is required: git blocks local-path submodule sources by default.
        await RunGitAsync("-c", "protocol.file.allow=always", "submodule", "add", "--quiet", sourceRepository._rootDirectory, name);
        await GitRepositoryBuilder.CommitAllAsync(_rootDirectory, $"Add submodule {name}");

        string destination = Path.Combine(_rootDirectory, name);
        TestProject submoduleTestApp = await WriteDefaultTestAppAsync(destination);
        return new GitRepository(workspace, destination, submoduleTestApp);
    }

    public void DeleteSharedCache()
    {
        File.Delete(_sharedCacheFilePath);
    }

    public RemotePushProjectTree SimulatePush(string name)
    {
        string destination = workspace.GetPath(name);
        GitRepositoryBuilder.SimulateSourcePush(_rootDirectory, destination);

        var pushedTestApp = new TestProject(Path.Combine(destination, GitPropertiesTestWorkspace.TestAppProjectName),
            GitPropertiesTestWorkspace.TestAppProjectName);

        return new RemotePushProjectTree(destination, pushedTestApp);
    }

    internal static async Task<TestProject> WriteDefaultTestAppAsync(string repositoryDirectory)
    {
        string appDirectory = await TestProjectWriter.WriteDefaultTestAppProjectAsync(repositoryDirectory);
        return new TestProject(appDirectory, GitPropertiesTestWorkspace.TestAppProjectName);
    }
}
