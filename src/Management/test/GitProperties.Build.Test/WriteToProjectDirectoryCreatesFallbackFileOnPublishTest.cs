// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class WriteToProjectDirectoryCreatesFallbackFileOnPublishTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// "dotnet publish" runs its own compile/composition steps internally regardless of whether "dotnet build" ran first - this guards against the fallback
    /// file only being written along the "build" target chain and silently never firing when publish is the very first command run against a fresh checkout
    /// (a common real-world pattern: `dotnet publish` directly, without a separate build step). Runs with $(GitPropertiesEnableWarnings) at its default
    /// (enabled) setting to confirm nothing about the fallback-writing path implicitly depends on warnings being suppressed - since a real .git repository
    /// is available here, nothing should be skipped (and no GITPROPS0xx code should appear) regardless of that setting.
    /// </summary>
    [Fact]
    public async Task WriteToProjectDirectory_CreatesFallbackFile_OnPublish()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1, true);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        ProcessResult result = await ProcessRunner.RunDotnetAsync(testApp, "publish", "-p:GitPropertiesWriteToProjectDirectory=true",
            "-p:GitPropertiesEnableWarnings=true");

        AssertBuildSucceeded(result, "publish with GitPropertiesWriteToProjectDirectory=true, without an upfront build");

        result.Output.Should().NotContain("GITPROPS0",
            "nothing should be skipped, and no fallback should be needed, when a real .git repository is available.");

        File.Exists(GetFallbackFilePath(testApp)).Should().BeTrue("the fallback file should have been written next to the .csproj, even for a bare publish.");

        Dictionary<string, string> fallbackProperties = await PropertiesFile.ReadAsync(GetFallbackFilePath(testApp));
        Dictionary<string, string> publishedProperties = await PropertiesFile.ReadAsync(GetReleasePublishGitPropertiesFilePath(testApp));
        fallbackProperties.Should().BeEquivalentTo(publishedProperties, "the fallback file must carry the exact same content as the published output.");

        string gitStatus = await ProcessRunner.GetGitOutputAsync(repository, "status", "--porcelain");
        gitStatus.Should().BeEmpty("the fallback file is gitignored, so it must not show up as an untracked change.");
    }
}
