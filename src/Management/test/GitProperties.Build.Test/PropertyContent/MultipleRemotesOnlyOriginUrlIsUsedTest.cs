// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.PropertyContent;

public sealed class MultipleRemotesOnlyOriginUrlIsUsedTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        await repository.RunGitAsync("remote", "add", "upstream", "https://example.com/upstream.git");
        await repository.RunGitAsync("remote", "add", "origin", "https://example.com/origin.git");
        await repository.RunGitAsync("remote", "set-url", "--add", "origin", "https://user:pass@example.com/origin-second.git");
        await repository.TestApp.BuildAsync();
        Dictionary<string, string> properties = await repository.TestApp.ReadDebugPropertiesAsync();
        properties["git.remote.origin.url"].Should().Be("https://example.com/origin-second.git");

        await repository.RunGitAsync("remote", "remove", "origin");
        await repository.RunGitAsync("remote", "add", "origin", "git@github.com:org/repo.git");
        await repository.TestApp.BuildAsync();
        Dictionary<string, string> propertiesAfterScpStyleUrl = await repository.TestApp.ReadDebugPropertiesAsync();
        propertiesAfterScpStyleUrl["git.remote.origin.url"].Should().Be("git@github.com:org/repo.git");
    }
}
