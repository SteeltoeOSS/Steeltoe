// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class WriteGitPropertiesFallbackFileWorksWithNoRestoreTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// "--no-restore" must work the same way for this target as for any other build invocation - it only requires that restore already happened at least
    /// once, same as a normal build.
    /// </summary>
    [Fact]
    public async Task WriteGitPropertiesFallbackFile_WorksWithNoRestore()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1, true);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        await ProcessRunner.RunDotnetAsync(testApp, "restore");
        await ProcessRunner.RunDotnetAsync(testApp, "build", "--no-restore", "-t:WriteGitPropertiesFallbackFile");

        File.Exists(GetFallbackFilePath(testApp)).Should().BeTrue();
    }
}
