// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

/// <summary>
/// A synthetic git repository directory under test, with its default <see cref="TestApp" /> already written into it. Wraps the directory so a test can
/// run git commands, add further projects, or simulate a source-based push against it without re-deriving its path or reaching for
/// <see cref="GitRepositoryBuilder" />/<see cref="TestProjectWriter" /> itself. Created by <see cref="GitPropertiesTestWorkspace" /> or
/// <see cref="EmptyGitRepository.AddTestAppAsync" /> - never directly. For a repository that doesn't have (or need) a TestApp - a fully custom git setup
/// - see <see cref="GitPropertiesTestWorkspace.CreateEmptyRepositoryAsync" /> instead.
/// </summary>
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

    /// <summary>
    /// Adds the default TestApp project referencing <paramref name="dependency" /> via a normal &lt;ProjectReference&gt; (see
    /// <see cref="TestProject.ToProjectReferenceXml" />), with $(GenerateGitProperties) left unset so the smart default applies - the shared setup every
    /// SmartDefault*Test in this suite needs before overriding $(GitPropertiesConsumingPackageIds) or $(GenerateGitProperties) itself.
    /// </summary>
    public Task<TestProject> AddTestAppReferencingAsync(TestProject dependency)
    {
        string extraItemGroupContent = dependency.ToProjectReferenceXml();
        return AddProjectAsync(GitPropertiesTestWorkspace.TestAppProjectName, generateGitProperties: null, extraItemGroupContent: extraItemGroupContent);
    }

    /// <summary>
    /// A bare console app consuming Steeltoe.Management.GitProperties.Build via &lt;PackageReference&gt; - see
    /// <see cref="TestProjectWriter.CreatePackageConsumerProjectAsync" />. Placed inside this repository (not directly under the workspace root), so the
    /// repo-root walk that starts at the consumer project still finds this repository's own ".git" above it.
    /// </summary>
    public async Task<TestProject> AddPackageConsumerProjectAsync(string name, string packageVersion)
    {
        string projectDirectory = Path.Combine(rootDirectory, name);
        await TestProjectWriter.CreatePackageConsumerProjectAsync(projectDirectory, packageVersion);
        return new TestProject(projectDirectory, name);
    }

    /// <summary>
    /// A shallow (--depth) clone of this repository, with the CURRENT Steeltoe.Management.GitProperties.Build source and a fresh TestApp copied in - see
    /// <see cref="TestProjectWriter.CopyCurrentProjectFilesAsync" />. --no-local is required here: for a plain local filesystem path, git's local-clone
    /// optimization bypasses shallow-transfer logic entirely and --depth is silently ignored, producing a full clone that would make a shallow-clone test
    /// worthless.
    /// </summary>
    public async Task<GitRepository> CloneAsShallowAsync(string name, int depth = 1)
    {
        string destination = workspace.GetPath(name);
        await ProcessRunner.RunGitAsync(Path.GetTempPath(), "clone", "--quiet", "--no-local", "--depth", $"{depth}", rootDirectory, destination);

        TestProject shallowTestApp = await WriteDefaultTestAppAsync(destination);
        return new GitRepository(workspace, destination, shallowTestApp);
    }

    public RemotePushProjectTree SimulatePush(string name)
    {
        string destination = workspace.GetPath(name);
        string pushRoot = GitRepositoryBuilder.SimulateSourcePush(rootDirectory, destination);

        var pushedTestApp = new TestProject(Path.Combine(pushRoot, GitPropertiesTestWorkspace.TestAppProjectName),
            GitPropertiesTestWorkspace.TestAppProjectName);

        return new RemotePushProjectTree(pushRoot, pushedTestApp);
    }

    /// <summary>
    /// Copies the CURRENT Steeltoe.Management.GitProperties.Build source into <paramref name="repositoryDirectory" /> and writes the default TestApp project
    /// referencing it - the one piece <see cref="GitPropertiesTestWorkspace.CreateGitRepositoryAsync" /> and <see cref="CloneAsShallowAsync" /> (and, via
    /// <see cref="EmptyGitRepository.AddTestAppAsync" />, the fully-custom-setup path) all share.
    /// </summary>
    internal static async Task<TestProject> WriteDefaultTestAppAsync(string repositoryDirectory)
    {
        string appDirectory = await TestProjectWriter.CopyCurrentProjectFilesAsync(repositoryDirectory);
        return new TestProject(appDirectory, GitPropertiesTestWorkspace.TestAppProjectName);
    }
}
