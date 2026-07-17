// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class WriteToProjectDirectoryCreatesFallbackFileOnBuildTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task WriteToProjectDirectory_CreatesFallbackFile_OnBuild()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1, true);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        string result1 = await ProcessRunner.RunDotnetAsync(testApp, "build", "-p:GitPropertiesWriteToProjectDirectory=true");

        result1.Should().Contain(
            $"git.properties: writing fallback copy to '{GetFallbackFilePath(testApp)}' for project '{GitPropertiesTestWorkspace.TestAppProjectName}'.");

        File.Exists(GetFallbackFilePath(testApp)).Should().BeTrue("the fallback file should have been written next to the .csproj.");

        Dictionary<string, string> fallbackProperties = await PropertiesFile.ReadAsync(GetFallbackFilePath(testApp));
        Dictionary<string, string> outputProperties1 = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));
        fallbackProperties.Should().BeEquivalentTo(outputProperties1, "the fallback file must carry the exact same content as the live build output.");

        string gitStatus = await ProcessRunner.GetGitOutputAsync(repository, "status", "--porcelain");
        gitStatus.Should().BeEmpty("the fallback file is gitignored, so it must not show up as an untracked change.");

        // A gitignored fallback file left over from the first build must not itself make a LATER build see the tree as dirty.
        await ProcessRunner.RunDotnetAsync(testApp, "build", "-p:GitPropertiesWriteToProjectDirectory=true");

        Dictionary<string, string> outputProperties2 = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));

        outputProperties2["git.dirty"].Should().Be("false",
            "the gitignored fallback file from the first build must not make a later build see the tree as dirty.");
    }
}
