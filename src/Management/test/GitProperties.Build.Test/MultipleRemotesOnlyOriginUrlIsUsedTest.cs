// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class MultipleRemotesOnlyOriginUrlIsUsedTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// GenerateGitPropertiesCacheTask.ReadConfig only recognizes the literal "remote.origin.url" config key - a repository with additional remotes (a fork's
    /// "upstream", a CI mirror, etc.) must still resolve git.remote.origin.url to origin's own URL, never another remote's. Also confirms that when origin
    /// itself has more than one configured URL (via "git remote set-url --add"), the field resolves to the LAST one, matching "git config --list"'s own
    /// last-value-wins behavior for repeated keys (verified independently against a real git binary before writing this test). The winning URL is
    /// deliberately given embedded credentials, folding StripUserInfo's own coverage into this same build rather than spinning up a dedicated test just for
    /// that: proves credentials are stripped from whichever URL actually wins, not just from a hypothetical single-remote case.
    /// </summary>
    [Fact]
    public async Task MultipleRemotes_OnlyOriginUrlIsUsed()
    {
        string repository = await Workspace.CreateSyntheticRepoAsync(Path.Combine(Workspace.RootDirectory, "repo"), 1);
        string testApp = Path.Combine(repository, GitPropertiesTestWorkspace.TestAppProjectName);

        await ProcessRunner.RunGitAsync(repository, "remote", "add", "upstream", "https://example.com/upstream.git");
        await ProcessRunner.RunGitAsync(repository, "remote", "add", "origin", "https://example.com/origin.git");
        await ProcessRunner.RunGitAsync(repository, "remote", "set-url", "--add", "origin", "https://user:pass@example.com/origin-second.git");

        ProcessResult result = await ProcessRunner.RunDotnetAsync(testApp, "build");
        AssertBuildSucceeded(result, "build with multiple remotes configured");

        Dictionary<string, string> properties = await PropertiesFile.ReadAsync(GetDebugGitPropertiesFilePath(testApp));

        properties["git.remote.origin.url"].Should().Be("https://example.com/origin-second.git",
            "origin's own last-configured URL must win, ignoring both the unrelated 'upstream' remote and origin's own first URL - and its embedded " +
            "'user:pass@' credentials must be stripped before the value ever reaches the cache file.");
    }
}
