// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.Publishing;

public sealed class PublishNoBuildIncludesGitPropertiesTest : GitPropertiesTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        DotNetCommandOutput output = await repository.TestApp.BuildAsync("-c", "Release");
        output.Value.Should().Contain("0 Warning(s)");

        await repository.TestApp.PublishAsync("-c", "Release", "--no-build");
        Dictionary<string, string> properties = await repository.TestApp.ReadReleasePublishPropertiesAsync();
        string expectedCommitId = await repository.GetCommitIdAsync();
        properties["git.commit.id"].Should().Be(expectedCommitId);
    }
}
