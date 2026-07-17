// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class ShallowCloneLeavesCommitCountsEmptyTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task ShallowClone_LeavesCommitCountsEmpty()
    {
        string source = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "source"), 3);
        await ProcessRunner.RunGitAsync(source, "tag", "v1.0.0");

        string shallow = Path.Combine(Workspace.RootDirectory, "shallow");
        // --no-local is required here: for a plain local filesystem path, git's local-clone
        // optimization bypasses shallow-transfer logic entirely and --depth is silently ignored,
        // producing a full clone that would make this test worthless.
        await ProcessRunner.RunGitAsync(Path.GetTempPath(), "clone", "--quiet", "--no-local", "--depth", "1", source, shallow);
        string isShallowRepository = await ProcessRunner.GetGitOutputAsync(shallow, "rev-parse", "--is-shallow-repository");
        isShallowRepository.Should().Be("true");

        string testApp = await TestProjectWriter.CopyCurrentProjectFilesAsync(shallow);

        string result = await ProcessRunner.RunDotnetAsync(testApp, "build");
        result.Should().NotContain("GITPROPS001");
        result.Should().NotContain("GITPROPS002");
        AssertWarned(result, "GITPROPS006");

        Dictionary<string, string> properties = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));
        properties["git.total.commit.count"].Should().BeEmpty();
        properties["git.closest.tag.commit.count"].Should().BeEmpty();
    }
}
