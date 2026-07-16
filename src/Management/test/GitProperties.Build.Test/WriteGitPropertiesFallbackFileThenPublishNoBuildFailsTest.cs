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

        ProcessResult writeResult = await ProcessRunner.RunDotnetAsync(testApp, "build", "-t:WriteGitPropertiesFallbackFile");
        AssertBuildSucceeded(writeResult, "build -t:WriteGitPropertiesFallbackFile");

        ProcessResult publishResult = await ProcessRunner.RunDotnetAsync(testApp, "publish", "--no-build");

        publishResult.ExitCode.Should().NotBe(0,
            "publishing --no-build after only writing the fallback file (no real build ever ran) must fail - there is no compiled output to publish.");
    }
}
