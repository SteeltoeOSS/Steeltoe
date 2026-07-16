// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class FallbackFileIgnoredWhenLiveGitAvailableTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// Guards against a stale fallback file (left over from some earlier build) ever shadowing live generation - the fallback file must only ever be used as
    /// a last resort, never preferred over a real, currently-usable .git repository.
    /// </summary>
    [Fact]
    public async Task FallbackFile_Ignored_WhenLiveGitAvailable()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1, true);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        await File.WriteAllLinesAsync(GetFallbackFilePath(testApp), ["git.commit.id=deadbeefdeadbeefdeadbeefdeadbeefdeadbeef"],
            TestContext.Current.CancellationToken);

        ProcessResult result = await ProcessRunner.RunDotnetAsync(testApp, "build", "-v:detailed");
        AssertBuildSucceeded(result, "build with a stale fallback file present alongside a real .git repository");
        result.Output.Should().NotContain("using pre-generated fallback file", "the fallback notice must not appear when live generation actually ran.");

        Dictionary<string, string> properties = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));
        string expectedCommitId = await ProcessRunner.GetGitOutputAsync(repository, "rev-parse", "HEAD");
        properties["git.commit.id"].Should().Be(expectedCommitId);
    }
}
