// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class FallbackFileWithoutGitignoreMakesLaterBuildsAppearDirtyTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// The negative counterpart to <see cref="WriteToProjectDirectoryCreatesFallbackFileOnBuildTest" /> - proves the README's ".gitignore this file" warning
    /// is describing a real consequence, not a hypothetical one: deliberately uses a repository WITHOUT the fallback file gitignored, so the file the first
    /// build writes is left behind as a genuine untracked change - permanently flipping git.dirty to "true" on every later build, even though nothing about
    /// the actually-tracked source changed in between.
    /// </summary>
    [Fact]
    public async Task FallbackFile_WithoutGitignore_MakesLaterBuildsAppearDirty()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult result1 = await ProcessRunner.RunDotnetAsync(testApp, "build", "-p:GitPropertiesWriteToProjectDirectory=true");
        AssertBuildSucceeded(result1, "first build, which writes the (not yet gitignored) fallback file");

        string gitStatus = await ProcessRunner.GetGitOutputAsync(repository, "status", "--porcelain");
        gitStatus.Should().NotBeEmpty("the freshly-written, ungitignored fallback file should show up as an untracked change.");

        ProcessResult result2 = await ProcessRunner.RunDotnetAsync(testApp, "build", "-p:GitPropertiesWriteToProjectDirectory=true");
        AssertBuildSucceeded(result2, "second build");

        Dictionary<string, string> properties2 = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));

        properties2["git.dirty"].Should().Be("true",
            "the ungitignored fallback file left over from the first build makes every later build see the tree as dirty.");
    }
}
