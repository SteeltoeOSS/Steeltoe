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
    /// that: proves credentials are stripped from whichever URL actually wins, not just from a hypothetical single-remote case. A second build, later in
    /// this same test, also folds in coverage for an scp-style URL - the other shape StripUserInfo has to handle safely.
    /// </summary>
    [Fact]
    public async Task MultipleRemotes_OnlyOriginUrlIsUsed()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        await repository.RunGitAsync("remote", "add", "upstream", "https://example.com/upstream.git");
        await repository.RunGitAsync("remote", "add", "origin", "https://example.com/origin.git");
        await repository.RunGitAsync("remote", "set-url", "--add", "origin", "https://user:pass@example.com/origin-second.git");
        await repository.TestApp.BuildAsync();
        Dictionary<string, string> properties = await repository.TestApp.ReadDebugPropertiesAsync();

        properties["git.remote.origin.url"].Should().Be("https://example.com/origin-second.git",
            "origin's own last-configured URL must win, ignoring both the unrelated 'upstream' remote and origin's own first URL - and its embedded " +
            "'user:pass@' credentials must be stripped before the value ever reaches the cache file.");

        // A non-absolute, scp-style URL (git's other common remote syntax, alongside plain HTTPS/SSH URLs) isn't
        // something Uri can parse - proves StripUserInfo leaves it untouched rather than mangling or blanking it,
        // since there's nothing safe for it to rewrite. Reuses this same repository/build (a second build, not a
        // dedicated test) rather than paying for a whole new synthetic repo just to reconfigure one remote - only
        // possible because .git\config is itself a tracked _GitPropertiesCacheInputs entry, so this reconfiguration
        // alone (no new commit needed) is enough to force the cache to regenerate on the next build. Remove-then-add
        // rather than a plain "set-url": origin already carries two URLs from above (via "remote add" + "set-url
        // --add"), and git refuses a plain "set-url" against a remote with multiple values ("fatal: could not set
        // 'remote.origin.url' ... has multiple values") - remove-then-add is the clean way to replace all of them
        // with exactly one.
        await repository.RunGitAsync("remote", "remove", "origin");
        await repository.RunGitAsync("remote", "add", "origin", "git@github.com:org/repo.git");
        await repository.TestApp.BuildAsync();
        Dictionary<string, string> propertiesAfterScpStyleUrl = await repository.TestApp.ReadDebugPropertiesAsync();

        propertiesAfterScpStyleUrl["git.remote.origin.url"].Should().Be("git@github.com:org/repo.git",
            "a non-absolute, scp-style remote URL must be left exactly as-is - there is nothing safe for StripUserInfo to rewrite.");
    }
}
