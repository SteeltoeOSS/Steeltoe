// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

internal sealed class GitRepository(GitPropertiesTestWorkspace workspace, string rootDirectory, TestProject testApp)
{
    private readonly string _sharedCacheFilePath = Path.Combine(rootDirectory, "obj", "_GitProperties", "git.properties.cache");

    public TestProject TestApp { get; } = testApp;
    public bool SharedCacheExists => File.Exists(_sharedCacheFilePath);

    public Task<string> RunGitAsync(params string[] arguments)
    {
        return ProcessRunner.RunGitAsync(rootDirectory, arguments);
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

    public async Task<TestProject> AddProjectAsync(string name, string? targetFrameworks = null, bool? generateGitProperties = true,
        string? extraItemGroupContent = null)
    {
        string projectDirectory = await TestProjectWriter.WriteAppProjectAsync(rootDirectory, name, targetFrameworks, generateGitProperties,
            extraItemGroupContent);

        return new TestProject(projectDirectory, name);
    }

    public async Task<TestProject> AddDependencyProjectAsync(string name)
    {
        string projectDirectory = await TestProjectWriter.WriteDummyDependencyProjectAsync(rootDirectory, name);
        return new TestProject(projectDirectory, name);
    }

    public Task<TestProject> AddTestAppReferencingAsync(TestProject dependency)
    {
        string extraItemGroupContent = dependency.ToProjectReferenceXml();
        return AddProjectAsync(GitPropertiesTestWorkspace.TestAppProjectName, generateGitProperties: null, extraItemGroupContent: extraItemGroupContent);
    }

    public async Task<TestProject> AddPackageConsumerProjectAsync(string name, string packageVersion)
    {
        string projectDirectory = Path.Combine(rootDirectory, name);
        await TestProjectWriter.CreatePackageConsumerProjectAsync(projectDirectory, packageVersion);
        return new TestProject(projectDirectory, name);
    }

    public async Task<GitRepository> CloneAsShallowAsync(string name, int depth = 1)
    {
        string destination = workspace.GetPath(name);
        // --no-local is required: for a local path, git's local-clone optimization otherwise bypasses shallow-transfer logic entirely and silently ignores --depth, producing a full clone.
        await ProcessRunner.RunGitAsync(Path.GetTempPath(), "clone", "--quiet", "--no-local", "--depth", $"{depth}", rootDirectory, destination);

        TestProject shallowTestApp = await WriteDefaultTestAppAsync(destination);
        return new GitRepository(workspace, destination, shallowTestApp);
    }

    public void DeleteSharedCache()
    {
        File.Delete(_sharedCacheFilePath);
    }

    public RemotePushProjectTree SimulatePush(string name)
    {
        string destination = workspace.GetPath(name);
        string pushRoot = GitRepositoryBuilder.SimulateSourcePush(rootDirectory, destination);

        var pushedTestApp = new TestProject(Path.Combine(pushRoot, GitPropertiesTestWorkspace.TestAppProjectName),
            GitPropertiesTestWorkspace.TestAppProjectName);

        return new RemotePushProjectTree(pushRoot, pushedTestApp);
    }

    internal static async Task<TestProject> WriteDefaultTestAppAsync(string repositoryDirectory)
    {
        string appDirectory = await TestProjectWriter.CopyCurrentProjectFilesAsync(repositoryDirectory);
        return new TestProject(appDirectory, GitPropertiesTestWorkspace.TestAppProjectName);
    }
}
