// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class WriteGitPropertiesFallbackFileThenPublishNoBuildFailsTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// Documents/guards the one real caveat called out in PackageReadme.md: this target never produces real build output, so a local "dotnet publish
    /// --no-build" afterward must fail - there is nothing compiled to publish. If this target's own implementation ever accidentally started producing
    /// compiled output (defeating its "lightweight" purpose), this test would start failing for the opposite reason (publish --no-build would start
    /// succeeding) - a signal to revisit the target, not just delete this test.
    /// </summary>
    [Fact]
    public async Task WriteGitPropertiesFallbackFile_ThenPublishNoBuild_Fails()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1, true);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        await ProcessRunner.RunDotnetAsync(testApp, "build", "-t:WriteGitPropertiesFallbackFile");

        // 1, not just "nonzero": MSBuild's own long-standing, stable convention for "the build failed" (verified
        // against a real "dotnet publish --no-build" in this exact no-compiled-output scenario) - checked here,
        // at the point of the call, rather than via a separate assertion afterward.
        await ProcessRunner.RunDotnetAsync(testApp, 1, "publish", "--no-build");
    }
}
