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
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        await repository.TestApp.BuildAsync("-p:GitPropertiesWriteToProjectDirectory=true");

        bool isDirty = await repository.IsDirtyAsync();
        isDirty.Should().BeTrue("the freshly-written, ungitignored fallback file should show up as an untracked change.");

        await repository.TestApp.BuildAsync("-p:GitPropertiesWriteToProjectDirectory=true");

        Dictionary<string, string> properties2 = await repository.TestApp.ReadDebugPropertiesAsync();

        properties2["git.dirty"].Should().Be("true",
            "the ungitignored fallback file left over from the first build makes every later build see the tree as dirty.");
    }
}
